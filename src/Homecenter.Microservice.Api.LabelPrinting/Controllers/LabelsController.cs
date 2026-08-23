using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homecenter.Microservice.Api.LabelPrinting.Controllers;

/// <summary>
/// Consulta de etiquetas pre-generadas. Alimenta la vista previa del frontend
/// antes de que el operario confirme la impresion.
/// </summary>
[ApiController]
[Route("api/labels")]
[Authorize]
public sealed class LabelsController : ControllerBase
{
    private readonly IResolveLabelUseCase _resolveLabel;

    /// <summary>Crea el controlador con su caso de uso.</summary>
    /// <param name="resolveLabel">Caso de uso de resolucion de etiquetas.</param>
    public LabelsController(IResolveLabelUseCase resolveLabel)
    {
        _resolveLabel = resolveLabel;
    }

    /// <summary>Resuelve una ETQ/LPN con su documento, productos y disponibilidad por zona.</summary>
    [HttpGet("{lpn}")]
    [ProducesResponseType(typeof(ApiResponse<LabelDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LabelDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLpnAsync(
        string lpn,
        [FromQuery] string? zoneCode,
        CancellationToken cancellationToken)
    {
        var result = await _resolveLabel.ExecuteAsync(lpn, zoneCode, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }

        // Aqui si aplica 404: se consulto un recurso que no existe. El 200 con
        // success=false se reserva para las decisiones de negocio al imprimir.
        return NotFound(result);
    }
}
