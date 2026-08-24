using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

/// <summary>
/// Resolucion de las reimpresiones que quedaron esperando autorizacion.
/// </summary>
public interface IResolveReprintApprovalUseCase
{
    /// <summary>Consulta la bandeja de reimpresiones pendientes.</summary>
    /// <param name="page">Pagina solicitada, base 1.</param>
    /// <param name="pageSize">Registros por pagina.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pendientes ordenados por antiguedad.</returns>
    Task<ApiResponse<IReadOnlyCollection<PrintHistoryItemDto>>> GetPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Autoriza una reimpresion pendiente y, si las reglas siguen cumpliendose, imprime.
    /// </summary>
    /// <param name="requestId">Solicitud pendiente a autorizar.</param>
    /// <param name="decision">Comentario del autorizador.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Resultado final de la solicitud.</returns>
    Task<ApiResponse<PrintResultDto>> ApproveAsync(
        int requestId,
        ReprintDecisionDto decision,
        CancellationToken cancellationToken = default);

    /// <summary>Niega una reimpresion pendiente dejando el motivo registrado.</summary>
    /// <param name="requestId">Solicitud pendiente a negar.</param>
    /// <param name="decision">Motivo del rechazo, obligatorio.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Resultado final de la solicitud.</returns>
    Task<ApiResponse<PrintResultDto>> RejectAsync(
        int requestId,
        ReprintDecisionDto decision,
        CancellationToken cancellationToken = default);
}
