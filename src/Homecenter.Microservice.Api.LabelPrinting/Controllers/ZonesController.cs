using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Catalog;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homecenter.Microservice.Api.LabelPrinting.Controllers;

/// <summary>
/// Catalogo de zonas logisticas. Alimenta el selector de zona del frontend,
/// para que el operario no tenga que escribir el codigo a mano.
/// </summary>
[ApiController]
[Route("api/zones")]
[Authorize]
public sealed class ZonesController : ControllerBase
{
    private readonly IZoneRepository _zoneRepository;

    /// <summary>Crea el controlador con su repositorio.</summary>
    /// <param name="zoneRepository">Repositorio de zonas logisticas.</param>
    public ZonesController(IZoneRepository zoneRepository)
    {
        _zoneRepository = zoneRepository;
    }

    /// <summary>Lista las zonas logisticas activas.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Zonas disponibles para seleccionar al imprimir.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ZoneDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var zones = await _zoneRepository.GetAllAsync(cancellationToken);

        var payload = zones
            .Select(x => new ZoneDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToArray();

        return Ok(ApiResponse<IReadOnlyCollection<ZoneDto>>.Ok(payload));
    }
}
