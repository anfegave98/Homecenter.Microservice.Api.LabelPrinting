using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

/// <summary>Consulta del historial de impresiones y reimpresiones.</summary>
public interface IGetPrintHistoryUseCase
{
    /// <summary>Ejecuta la consulta aplicando las restricciones de visibilidad por rol.</summary>
    /// <param name="filter">Filtros y paginacion solicitados.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de registros de auditoria visible para el usuario autenticado.</returns>
    Task<ApiResponse<IReadOnlyCollection<PrintHistoryItemDto>>> ExecuteAsync(
        PrintHistoryFilterDto filter,
        CancellationToken cancellationToken = default);
}
