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

    /// <summary>
    /// Consulta las reimpresiones que esperan decision de un autorizador.
    /// </summary>
    /// <param name="page">Pagina solicitada, base 1.</param>
    /// <param name="pageSize">Registros por pagina.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de pendientes, de la mas antigua a la mas reciente.</returns>
    Task<PagedResult<PrintHistoryItemDto>> GetPendingApprovalsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera una solicitud que siga pendiente de autorizacion.
    /// </summary>
    /// <param name="id">Identificador de la solicitud en la auditoria.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>
    /// La solicitud con su solicitante cargado, o null si no existe o ya fue resuelta.
    /// Devolver null en ambos casos es deliberado: quien consulta no necesita saber si
    /// el identificador existio, solo que ya no hay nada que decidir.
    /// </returns>
    Task<PrintRequest?> GetPendingByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera una solicitud aprobada para entregar su etiqueta.
    /// </summary>
    /// <param name="id">Identificador de la solicitud en la auditoria.</param>
    /// <param name="restrictToUserId">
    /// Cuando tiene valor, la solicitud debe pertenecer a ese usuario. Lo impone el caso
    /// de uso segun el rol: un operario no descarga la etiqueta de otro.
    /// </param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La solicitud aprobada, o null si no existe o no le corresponde.</returns>
    Task<PrintRequest?> GetApprovedForDownloadAsync(
        int id,
        int? restrictToUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste la decision tomada sobre una solicitud pendiente.</summary>
    /// <param name="request">Solicitud con su desenlace y autorizador ya asignados.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task UpdateAsync(PrintRequest request, CancellationToken cancellationToken = default);

    /// <summary>Calcula los indicadores operativos de impresion.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Totales de solicitudes, aprobaciones, rechazos y reimpresiones.</returns>
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
