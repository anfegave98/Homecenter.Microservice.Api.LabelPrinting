using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Microsoft.Extensions.Logging;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;

/// <summary>
/// Procesa una solicitud de impresion sobre una ETQ/LPN pre-generada.
///
/// Flujo: resolver etiqueta y zona, cargar inventario, evaluar reglas, imprimir si
/// procede y auditar SIEMPRE. La auditoria no es condicional al exito porque los
/// rechazos son justamente lo que se investiga durante un incidente.
/// </summary>
public sealed class ProcessPrintRequestUseCase : IProcessPrintRequestUseCase
{
    private readonly ILabelRepository _labelRepository;
    private readonly IZoneRepository _zoneRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPrintRequestRepository _printRequestRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPrintSimulator _printSimulator;
    private readonly PrintRuleEngine _ruleEngine;
    private readonly ILogger<ProcessPrintRequestUseCase> _logger;

    /// <summary>Crea una instancia con sus dependencias.</summary>
    public ProcessPrintRequestUseCase(
        ILabelRepository labelRepository,
        IZoneRepository zoneRepository,
        IInventoryRepository inventoryRepository,
        IPrintRequestRepository printRequestRepository,
        ICurrentUserAccessor currentUser,
        IPrintSimulator printSimulator,
        PrintRuleEngine ruleEngine,
        ILogger<ProcessPrintRequestUseCase> logger)
    {
        _labelRepository = labelRepository;
        _zoneRepository = zoneRepository;
        _inventoryRepository = inventoryRepository;
        _printRequestRepository = printRequestRepository;
        _currentUser = currentUser;
        _printSimulator = printSimulator;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<PrintResultDto>> ExecuteAsync(
        PrintRequestCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        var context = await BuildContextAsync(request, cancellationToken);
        var evaluation = _ruleEngine.Evaluate(context);

        var eventType = context.HasPreviousPrint ? PrintEventType.Reprint : PrintEventType.Print;

        string? zpl = null;
        if (evaluation.IsApproved)
        {
            zpl = await _printSimulator.PrintAsync(context.Label!, correlationId, cancellationToken);
        }

        var printRequest = await PersistAuditAsync(
            correlationId,
            request,
            context,
            evaluation,
            eventType,
            cancellationToken);

        var payload = BuildResult(correlationId, context, evaluation, eventType, zpl, printRequest.ProcessedAt);

        if (evaluation.IsApproved)
        {
            _logger.LogInformation(
                "Impresion aprobada. CorrelationId={CorrelationId} Lpn={Lpn} Zona={Zona} Usuario={Usuario} Tipo={Tipo}",
                correlationId, context.RequestedKey, context.Zone?.Code, context.UserName, eventType);

            return ApiResponse<PrintResultDto>.Ok(payload);
        }

        var failure = evaluation.Failure!;

        _logger.LogWarning(
            "Impresion rechazada. CorrelationId={CorrelationId} Lpn={Lpn} Regla={Regla} Motivo={Motivo}",
            correlationId, context.RequestedKey, failure.RuleCode, failure.RejectionCode);

        // Rechazo de negocio: HTTP 200 con success=false. No es un error tecnico,
        // es la respuesta correcta del dominio a una solicitud que no cumple reglas.
        return ApiResponse<PrintResultDto>.Fail(
            new ApiError
            {
                Code = failure.RejectionCode!,
                Message = failure.Message!,
                Details = failure.Details
            },
            payload);
    }

    private async Task<PrintRuleContext> BuildContextAsync(
        PrintRequestCreateDto request,
        CancellationToken cancellationToken)
    {
        var label = await _labelRepository.GetByLpnOrEtqAsync(request.Lpn, cancellationToken);
        var document = label?.Document;

        // Si el operario no indica zona, se usa la del documento origen.
        var zone = await ResolveZoneAsync(request.ZoneCode, document, cancellationToken);

        var products = document?.DocumentProducts.Where(x => x.State).ToArray() ?? Array.Empty<DocumentProduct>();

        var availability = new Dictionary<int, InventoryAvailability>();
        if (zone is not null && products.Length > 0)
        {
            var rows = await _inventoryRepository.GetByProductsAndZoneAsync(
                products.Select(x => x.IdProduct).Distinct().ToArray(),
                zone.Id,
                cancellationToken);

            availability = rows.ToDictionary(x => x.IdProduct);
        }

        var hasPreviousPrint = label is not null
            && await _printRequestRepository.HasApprovedPrintAsync(label.LpnId, cancellationToken);

        return new PrintRuleContext
        {
            RequestedKey = request.Lpn,
            RequestedZoneCode = request.ZoneCode ?? document?.Zone?.Code,
            ReprintReason = request.ReprintReason,
            UserName = _currentUser.UserName ?? string.Empty,
            UserRoles = _currentUser.Roles,
            Label = label,
            Document = document,
            Zone = zone,
            Products = products,
            Availability = availability,
            HasPreviousPrint = hasPreviousPrint
        };
    }

    private async Task<Zone?> ResolveZoneAsync(
        string? requestedZoneCode,
        Document? document,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedZoneCode))
        {
            return await _zoneRepository.GetByCodeAsync(requestedZoneCode.Trim(), cancellationToken);
        }

        return document?.Zone;
    }

    private async Task<PrintRequest> PersistAuditAsync(
        Guid correlationId,
        PrintRequestCreateDto request,
        PrintRuleContext context,
        PrintRuleEvaluation evaluation,
        PrintEventType eventType,
        CancellationToken cancellationToken)
    {
        var failure = evaluation.Failure;

        var printRequest = new PrintRequest
        {
            CorrelationId = correlationId,
            EtqId = context.Label?.EtqId,
            LpnId = string.IsNullOrWhiteSpace(request.Lpn) ? "(vacio)" : request.Lpn,
            IdZone = context.Zone?.Id,
            IdUser = _currentUser.UserId ?? 0,
            DocumentNumber = context.Document?.DocumentNumber,
            Result = evaluation.IsApproved ? PrintResult.Approved : PrintResult.Rejected,
            EventType = eventType,
            RejectionCode = failure?.RejectionCode,
            RejectionMessage = Truncate(failure?.Message, 500),
            ReprintReason = eventType == PrintEventType.Reprint ? request.ReprintReason : null,
            ProcessedAt = DateTimeOffset.UtcNow
        };

        foreach (var step in evaluation.Trace)
        {
            printRequest.AuditLogs.Add(new PrintAuditLog
            {
                RuleCode = step.RuleCode,
                Passed = step.Passed,
                Detail = Truncate(step.Message, 500),
                EvaluatedAt = DateTimeOffset.UtcNow
            });
        }

        await _printRequestRepository.AddAsync(printRequest, cancellationToken);
        return printRequest;
    }

    private static PrintResultDto BuildResult(
        Guid correlationId,
        PrintRuleContext context,
        PrintRuleEvaluation evaluation,
        PrintEventType eventType,
        string? zpl,
        DateTimeOffset processedAt)
    {
        var products = context.Products
            .Select(line =>
            {
                context.Availability.TryGetValue(line.IdProduct, out var availability);
                var availableQty = availability?.AvailableQty ?? 0m;
                var isStocked = availability?.IsStocked ?? false;

                return new ProductAvailabilityDto
                {
                    ProductCode = line.Product?.ProductCode ?? string.Empty,
                    ProductDescription = line.Product?.ProductDescription ?? string.Empty,
                    RequestedQty = line.RequestedQty,
                    Uom = line.Uom,
                    AvailableQty = availableQty,
                    IsStocked = isStocked,
                    IsEligible = availableQty >= line.RequestedQty && isStocked
                };
            })
            .ToArray();

        return new PrintResultDto
        {
            CorrelationId = correlationId,
            Result = evaluation.IsApproved ? nameof(PrintResult.Approved).ToUpperInvariant() : "REJECTED",
            EventType = eventType == PrintEventType.Reprint ? "REPRINT" : "PRINT",
            EtqId = context.Label?.EtqId,
            LpnId = context.RequestedKey,
            ZoneCode = context.Zone?.Code,
            UserName = context.UserName,
            DocumentNumber = context.Document?.DocumentNumber,
            ProcessedAt = processedAt,
            ReprintReason = eventType == PrintEventType.Reprint ? context.ReprintReason : null,
            Zpl = zpl,
            Products = products.Length > 0 ? products : null,
            Legacy = evaluation.IsApproved ? BuildLegacy(context, zpl) : null
        };
    }

    /// <summary>
    /// Construye el bloque compatible con responseEtq.json.
    /// El anexo expone un unico SKU escalar, asi que se toma el primer producto y se
    /// declara si habia mas: asi el consumidor legacy no rompe, pero tampoco se le
    /// oculta que la ETQ arrastra mas productos de los que ese contrato puede mostrar.
    /// </summary>
    private static LegacyEtqResponseDto? BuildLegacy(PrintRuleContext context, string? zpl)
    {
        if (context.Label is null || context.Document is null)
        {
            return null;
        }

        var first = context.Products.FirstOrDefault();

        return new LegacyEtqResponseDto
        {
            IdEtiqueta = context.Label.EtqId,
            PurchaseOrder = context.Document.DocumentNumber,
            TcOrderId = context.Document.RequestId,
            Sku = first?.Product?.ProductCode ?? string.Empty,
            Unidades = first?.RequestedQty ?? 0m,
            Zpl = zpl ?? context.Label.Zpl,
            HasMultipleProducts = context.Products.Count > 1
        };
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
