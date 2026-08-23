using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

/// <summary>Indicadores operativos del submodulo de impresion.</summary>
public interface IGetDashboardUseCase
{
    /// <summary>Obtiene los totales de solicitudes, rechazos y reimpresiones.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Indicadores agregados.</returns>
    Task<ApiResponse<AdminDashboardDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}
