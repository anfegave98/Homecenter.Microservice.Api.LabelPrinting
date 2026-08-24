using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Services;
using Microsoft.Extensions.Logging;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;

/// <summary>
/// Procesa una solicitud de impresion sobre una ETQ/LPN pre-generada.
///
/// Flujo: resolver etiqueta y zona, cargar inventario, evaluar reglas, imprimir si
/// procede y auditar SIEMPRE. La auditoria no es condicional al exito porque los
/// rechazos son justamente lo que se investiga durante un incidente.
///
/// La solicitud tiene tres desenlaces, no dos: puede quedar pendiente cuando es una
/// reimpresion pedida por alguien sin rol autorizado.
/// </summary>
public sealed class ProcessPrintRequestUseCase : IProcessPrintRequestUseCase
{
    private readonly IPrintRequestRepository _printRequestRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPrintSimulator _printSimulator;
    private readonly PrintContextBuilder _contextBuilder;
    private readonly PrintRuleEngine _ruleEngine;
    private readonly ILogger<ProcessPrintRequestUseCase> _logger;

    /// <summary>Crea una instancia con sus dependencias.</summary>
    /// <param name="printRequestRepository">Acceso a la auditoria de impresiones.</param>
    /// <param name="currentUser">Identidad tomada del token.</param>
    /// <param name="printSimulator">Simulador que genera el evento de impresion.</param>
    /// <param name="contextBuilder">Constructor del contexto que consumen las reglas.</param>
    /// <param name="ruleEngine">Motor que evalua las reglas en orden.</param>
    /// <param name="logger">Registro de eventos.</param>
    public ProcessPrintRequestUseCase(
        IPrintRequestRepository printRequestRepository,
        ICurrentUserAccessor currentUser,
        IPrintSimulator printSimulator,
        PrintContextBuilder contextBuilder,
        PrintRuleEngine ruleEngine,
        ILogger<ProcessPrintRequestUseCase> logger)
    {
        _printRequestRepository = printRequestRepository;
        _currentUser = currentUser;
        _printSimulator = printSimulator;
        _contextBuilder = contextBuilder;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<PrintResultDto>> ExecuteAsync(
        PrintRequestCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();

        var context = await _contextBuilder.BuildAsync(
            request.Lpn,
            request.ZoneCode,
            request.ReprintReason,
            cancellationToken);

        var evaluation = _ruleEngine.Evaluate(context);

        var eventType = context.HasPreviousPrint ? PrintEventType.Reprint : PrintEventType.Print;
        var result = ResolveResult(evaluation);

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
            result,
            cancellationToken);

        var payload = PrintResultMapper.Build(printRequest, context, evaluation, zpl);

        if (evaluation.IsApproved)
        {
            _logger.LogInformation(
                "Impresion aprobada. CorrelationId={CorrelationId} Lpn={Lpn} Zona={Zona} Usuario={Usuario} Tipo={Tipo}",
                correlationId, context.RequestedKey, context.Zone?.Code, context.UserName, eventType);

            return ApiResponse<PrintResultDto>.Ok(payload);
        }

        var failure = evaluation.Failure!;

        if (evaluation.RequiresApproval)
        {
            _logger.LogInformation(
                "Reimpresion pendiente de autorizacion. CorrelationId={CorrelationId} Solicitud={Id} Lpn={Lpn} Usuario={Usuario}",
                correlationId, printRequest.Id, context.RequestedKey, context.UserName);
        }
        else
        {
            _logger.LogWarning(
                "Impresion rechazada. CorrelationId={CorrelationId} Lpn={Lpn} Regla={Regla} Motivo={Motivo}",
                correlationId, context.RequestedKey, failure.RuleCode, failure.RejectionCode);
        }

        // Rechazo de negocio y derivacion a autorizacion comparten envelope: HTTP 200 con
        // success=false. Ninguno es un error tecnico y en ninguno se entrego ZPL. El
        // consumidor los distingue por el codigo, no por el codigo HTTP.
        return ApiResponse<PrintResultDto>.Fail(
            new ApiError
            {
                Code = failure.RejectionCode!,
                Message = failure.Message!,
                Details = failure.Details
            },
            payload);
    }

    private static PrintResult ResolveResult(PrintRuleEvaluation evaluation)
    {
        if (evaluation.IsApproved)
        {
            return PrintResult.Approved;
        }

        return evaluation.RequiresApproval ? PrintResult.PendingApproval : PrintResult.Rejected;
    }

    private async Task<PrintRequest> PersistAuditAsync(
        Guid correlationId,
        PrintRequestCreateDto request,
        PrintRuleContext context,
        PrintRuleEvaluation evaluation,
        PrintEventType eventType,
        PrintResult result,
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
            Result = result,
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

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
