using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;

/// <summary>
/// Consulta del historial de impresiones y reimpresiones.
///
/// La visibilidad depende del rol y se decide aqui, no en el frontend: un operario
/// ve unicamente sus propias solicitudes, mientras que supervisor y administrador ven
/// la operacion completa. Ocultar filas solo en la interfaz dejaria los datos
/// accesibles para cualquiera que llame al endpoint directamente.
/// </summary>
public sealed class GetPrintHistoryUseCase : IGetPrintHistoryUseCase
{
    private readonly IPrintRequestRepository _printRequestRepository;
    private readonly ICurrentUserAccessor _currentUser;

    /// <summary>Crea el caso de uso con sus dependencias.</summary>
    /// <param name="printRequestRepository">Repositorio de auditoria de impresiones.</param>
    /// <param name="currentUser">Identidad del usuario autenticado.</param>
    public GetPrintHistoryUseCase(
        IPrintRequestRepository printRequestRepository,
        ICurrentUserAccessor currentUser)
    {
        _printRequestRepository = printRequestRepository;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IReadOnlyCollection<PrintHistoryItemDto>>> ExecuteAsync(
        PrintHistoryFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var seesEverything = _currentUser.IsInRole(RoleName.Supervisor)
                          || _currentUser.IsInRole(RoleName.Admin);

        var restrictToUserId = seesEverything ? null : _currentUser.UserId;

        var page = await _printRequestRepository.GetHistoryAsync(filter, restrictToUserId, cancellationToken);

        return ApiResponse<IReadOnlyCollection<PrintHistoryItemDto>>.Ok(
            page.Items,
            new
            {
                total = page.Total,
                page = page.Page,
                pageSize = page.PageSize,
                totalPages = page.TotalPages,
                scope = seesEverything ? "ALL" : "OWN"
            });
    }
}
