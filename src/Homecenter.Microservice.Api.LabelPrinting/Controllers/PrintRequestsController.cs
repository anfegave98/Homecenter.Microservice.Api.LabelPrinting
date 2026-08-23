using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homecenter.Microservice.Api.LabelPrinting.Controllers;

[ApiController]
[Route("api/print-requests")]
[Authorize]
public sealed class PrintRequestsController : ControllerBase
{
    private readonly IProcessPrintRequestUseCase _processPrintRequest;

    public PrintRequestsController(IProcessPrintRequestUseCase processPrintRequest)
    {
        _processPrintRequest = processPrintRequest;
    }

    /// <summary>
    /// Procesa una solicitud de impresion sobre una ETQ/LPN pre-generada.
    /// Un rechazo de negocio responde 200 con success=false y el motivo: la solicitud
    /// se proceso correctamente, lo que no procede es la impresion.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PrintResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessAsync(
        [FromBody] PrintRequestCreateDto request,
        CancellationToken cancellationToken)
    {
        var result = await _processPrintRequest.ExecuteAsync(request, cancellationToken);
        return Ok(result);
    }
}
