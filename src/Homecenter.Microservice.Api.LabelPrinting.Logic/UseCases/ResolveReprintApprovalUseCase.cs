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
/// Resuelve las reimpresiones que quedaron esperando autorizacion.
///
/// La decision se toma sobre la misma fila que creo el operario, no sobre una nueva:
/// una solicitud es un solo hecho operativo y partirla en dos registros haria imposible
/// responder cuanto tardo en atenderse.
///
/// Aprobar no imprime a ciegas. El motor de reglas se vuelve a ejecutar con los datos
/// del momento de la decision, porque entre la solicitud y la autorizacion pudo pasar
/// cualquier cosa: el documento pudo anularse o el inventario de la zona agotarse. Un
/// visto bueno no puede convertir una impresion invalida en valida.
/// </summary>
public sealed class ResolveReprintApprovalUseCase : IResolveReprintApprovalUseCase
{
    private readonly IPrintRequestRepository _printRequestRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPrintSimulator _printSimulator;
    private readonly PrintContextBuilder _contextBuilder;
    private readonly PrintRuleEngine _ruleEngine;
    private readonly ILogger<ResolveReprintApprovalUseCase> _logger;

    /// <summary>Crea el caso de uso con sus dependencias.</summary>
    /// <param name="printRequestRepository">Acceso a la auditoria de impresiones.</param>
    /// <param name="currentUser">Identidad del autorizador, tomada del token.</param>
    /// <param name="printSimulator">Simulador que genera el evento de impresion.</param>
    /// <param name="contextBuilder">Constructor del contexto que consumen las reglas.</param>
    /// <param name="ruleEngine">Motor que evalua las reglas en orden.</param>
    /// <param name="logger">Registro de eventos.</param>
    public ResolveReprintApprovalUseCase(
        IPrintRequestRepository printRequestRepository,
        ICurrentUserAccessor currentUser,
        IPrintSimulator printSimulator,
        PrintContextBuilder contextBuilder,
        PrintRuleEngine ruleEngine,
        ILogger<ResolveReprintApprovalUseCase> logger)
    {
        _printRequestRepository = printRequestRepository;
        _currentUser = currentUser;
        _printSimulator = printSimulator;
        _contextBuilder = contextBuilder;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IReadOnlyCollection<PrintHistoryItemDto>>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _printRequestRepository.GetPendingApprovalsAsync(page, pageSize, cancellationToken);

        return ApiResponse<IReadOnlyCollection<PrintHistoryItemDto>>.Ok(
            result.Items,
            new
            {
                total = result.Total,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages = result.TotalPages
            });
    }

    /// <inheritdoc />
    public async Task<ApiResponse<PrintResultDto>> ApproveAsync(
        int requestId,
        ReprintDecisionDto decision,
        CancellationToken cancellationToken = default)
    {
        var pending = await _printRequestRepository.GetPendingByIdAsync(requestId, cancellationToken);

        if (pending is null)
        {
            return NotFound(requestId);
        }

        // Se reconstruye el contexto con los datos de hoy y se vuelve a evaluar todo,
        // no solo la regla de reimpresion.
        var context = await _contextBuilder.BuildAsync(
            pending.LpnId,
            pending.Zone?.Code,
            pending.ReprintReason,
            cancellationToken);

        var evaluation = _ruleEngine.Evaluate(context);

        string? zpl = null;

        if (evaluation.IsApproved)
        {
            zpl = await _printSimulator.PrintAsync(context.Label!, pending.CorrelationId, cancellationToken);

            pending.Result = PrintResult.Approved;
            pending.RejectionCode = null;
            pending.RejectionMessage = null;
        }
        else
        {
            // Las reglas dejaron de cumplirse entre la solicitud y la decision. Se cierra
            // como rechazo con el motivo real, no con el visto bueno del supervisor: lo
            // que impide imprimir es el estado de los datos, no su decision.
            var failure = evaluation.Failure!;
            pending.Result = PrintResult.Rejected;
            pending.RejectionCode = failure.RejectionCode;
            pending.RejectionMessage = Truncate(failure.Message, 500);
        }

        RecordDecision(pending, decision.Note);
        AppendDecisionTrace(pending, evaluation);

        await _printRequestRepository.UpdateAsync(pending, cancellationToken);

        _logger.LogInformation(
            "Reimpresion resuelta. Solicitud={Id} Lpn={Lpn} Autorizador={Autorizador} Desenlace={Desenlace}",
            pending.Id, pending.LpnId, _currentUser.UserName, pending.Result);

        var payload = PrintResultMapper.Build(
            pending,
            context,
            evaluation,
            zpl,
            pending.User?.UserName,
            _currentUser.UserName);

        if (evaluation.IsApproved)
        {
            return ApiResponse<PrintResultDto>.Ok(payload);
        }

        return ApiResponse<PrintResultDto>.Fail(
            new ApiError
            {
                Code = pending.RejectionCode!,
                Message = pending.RejectionMessage!,
                Details = evaluation.Failure!.Details
            },
            payload);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<PrintResultDto>> RejectAsync(
        int requestId,
        ReprintDecisionDto decision,
        CancellationToken cancellationToken = default)
    {
        // Negar sin explicacion deja al operario sin saber que corregir y a soporte sin
        // rastro de por que se nego el duplicado.
        if (string.IsNullOrWhiteSpace(decision.Note))
        {
            return ApiResponse<PrintResultDto>.Fail(
                RejectionCodes.ApprovalNoteRequired,
                "Debe indicar el motivo por el cual se niega la reimpresion.");
        }

        var pending = await _printRequestRepository.GetPendingByIdAsync(requestId, cancellationToken);

        if (pending is null)
        {
            return NotFound(requestId);
        }

        pending.Result = PrintResult.Rejected;
        pending.RejectionCode = RejectionCodes.ReprintRejectedByApprover;
        pending.RejectionMessage = Truncate(
            $"Reimpresion negada por {_currentUser.UserName}. Motivo: {decision.Note}",
            500);

        RecordDecision(pending, decision.Note);

        pending.AuditLogs.Add(new PrintAuditLog
        {
            RuleCode = RuleCodes.ReprintApproval,
            Passed = false,
            Detail = Truncate(pending.RejectionMessage, 500),
            EvaluatedAt = DateTimeOffset.UtcNow
        });

        await _printRequestRepository.UpdateAsync(pending, cancellationToken);

        _logger.LogInformation(
            "Reimpresion negada. Solicitud={Id} Lpn={Lpn} Autorizador={Autorizador}",
            pending.Id, pending.LpnId, _currentUser.UserName);

        return ApiResponse<PrintResultDto>.Fail(
            new ApiError
            {
                Code = pending.RejectionCode,
                Message = pending.RejectionMessage!
            },
            BuildDecisionOnlyResult(pending));
    }

    private void RecordDecision(PrintRequest pending, string? note)
    {
        pending.IdApprover = _currentUser.UserId;
        pending.DecidedAt = DateTimeOffset.UtcNow;
        pending.ApprovalNote = Truncate(note, 300);
    }

    /// <summary>
    /// Anexa a la solicitud original la traza de la segunda evaluacion.
    ///
    /// Los pasos de la primera se conservan: el historial debe mostrar que se valido al
    /// pedir y que se valido al autorizar, porque son dos momentos distintos y pueden
    /// haber dado resultados distintos.
    /// </summary>
    private static void AppendDecisionTrace(PrintRequest pending, PrintRuleEvaluation evaluation)
    {
        foreach (var step in evaluation.Trace)
        {
            pending.AuditLogs.Add(new PrintAuditLog
            {
                RuleCode = step.RuleCode,
                Passed = step.Passed,
                Detail = Truncate(step.Message, 500),
                EvaluatedAt = DateTimeOffset.UtcNow
            });
        }

        pending.AuditLogs.Add(new PrintAuditLog
        {
            RuleCode = RuleCodes.ReprintApproval,
            Passed = evaluation.IsApproved,
            Detail = evaluation.IsApproved
                ? "Reimpresion autorizada tras revalidar las reglas."
                : "Autorizacion recibida, pero las reglas dejaron de cumplirse.",
            EvaluatedAt = DateTimeOffset.UtcNow
        });
    }

    private PrintResultDto BuildDecisionOnlyResult(PrintRequest pending) =>
        new()
        {
            CorrelationId = pending.CorrelationId,
            RequestId = pending.Id,
            Result = PrintResultNames.Of(pending.Result),
            EventType = PrintResultNames.Of(pending.EventType),
            EtqId = pending.EtqId,
            LpnId = pending.LpnId,
            ZoneCode = pending.Zone?.Code,
            UserName = pending.User?.UserName ?? string.Empty,
            DocumentNumber = pending.DocumentNumber,
            ProcessedAt = pending.ProcessedAt,
            ReprintReason = pending.ReprintReason,
            ApprovedBy = _currentUser.UserName,
            DecidedAt = pending.DecidedAt,
            ApprovalNote = pending.ApprovalNote
        };

    private static ApiResponse<PrintResultDto> NotFound(int requestId) =>
        ApiResponse<PrintResultDto>.Fail(
            RejectionCodes.PendingRequestNotFound,
            $"La solicitud {requestId} no existe o ya fue resuelta por otro autorizador.");

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
