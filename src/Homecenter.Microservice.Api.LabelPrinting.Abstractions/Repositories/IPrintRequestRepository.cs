using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

/// <summary>
/// Acceso a las solicitudes de impresion procesadas, que constituyen la auditoria
/// del submodulo.
/// </summary>
public interface IPrintRequestRepository
{
    /// <summary>Persiste la solicitud junto con su traza de reglas.</summary>
    /// <param name="request">Solicitud a registrar, con sus logs de auditoria adjuntos.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task AddAsync(PrintRequest request, CancellationToken cancellationToken = default);

    /// <summary>Indica si existe una impresion aprobada previa para la ETQ/LPN (Regla 4).</summary>
    /// <param name="lpnId">Unidad logistica a verificar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>True si la etiqueta ya fue impresa con exito antes.</returns>
    Task<bool> HasApprovedPrintAsync(string lpnId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta paginada del historial.
    /// </summary>
    /// <param name="filter">Filtros y paginacion solicitados.</param>
    /// <param name="restrictToUserId">
    /// Cuando tiene valor, limita el resultado a las solicitudes de ese usuario.
    /// Lo impone el caso de uso segun el rol: no es un filtro que el cliente pueda elegir.
    /// </param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de resultados ordenada por fecha descendente.</returns>
    Task<PagedResult<PrintHistoryItemDto>> GetHistoryAsync(
        PrintHistoryFilterDto filter,
        int? restrictToUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Calcula los indicadores operativos de impresion.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Totales de solicitudes, aprobaciones, rechazos y reimpresiones.</returns>
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
