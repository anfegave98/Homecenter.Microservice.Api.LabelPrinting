using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Homecenter.Microservice.Api.LabelPrinting.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Homecenter.Microservice.Api.LabelPrinting.Controllers;

/// <summary>
/// Indicadores operativos del submodulo. Restringido al rol administrador.
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[EnableRateLimiting(RateLimitingSetup.QueryPolicy)]
[Authorize(Roles = RoleName.Admin)]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IGetDashboardUseCase _getDashboard;

    /// <summary>Crea el controlador con su caso de uso.</summary>
    /// <param name="getDashboard">Caso de uso de indicadores.</param>
    public AdminDashboardController(IGetDashboardUseCase getDashboard)
    {
        _getDashboard = getDashboard;
    }

    /// <summary>Consulta los totales de impresiones, rechazos y reimpresiones.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Indicadores agregados de la operacion.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var result = await _getDashboard.ExecuteAsync(cancellationToken);
        return Ok(result);
    }
}
