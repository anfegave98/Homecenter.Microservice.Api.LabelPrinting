using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;

/// <summary>
/// Indicadores operativos de impresion para el rol administrador.
/// </summary>
public sealed class GetDashboardUseCase : IGetDashboardUseCase
{
    private readonly IPrintRequestRepository _printRequestRepository;

    /// <summary>Crea el caso de uso con su repositorio.</summary>
    /// <param name="printRequestRepository">Repositorio de auditoria de impresiones.</param>
    public GetDashboardUseCase(IPrintRequestRepository printRequestRepository)
    {
        _printRequestRepository = printRequestRepository;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AdminDashboardDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var dashboard = await _printRequestRepository.GetDashboardAsync(cancellationToken);
        return ApiResponse<AdminDashboardDto>.Ok(dashboard);
    }
}
