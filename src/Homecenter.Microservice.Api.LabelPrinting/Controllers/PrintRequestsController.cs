using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Microsoft.AspNetCore.Authorization;
using Homecenter.Microservice.Api.LabelPrinting.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Homecenter.Microservice.Api.LabelPrinting.Controllers;

/// <summary>
/// Procesamiento de solicitudes de impresion y consulta de su historial.
/// </summary>
[ApiController]
[Route("api/print-requests")]
[EnableRateLimiting(RateLimitingSetup.PrintingPolicy)]
[Authorize]
public sealed class PrintRequestsController : ControllerBase
{
    private readonly IProcessPrintRequestUseCase _processPrintRequest;
    private readonly IGetPrintHistoryUseCase _getPrintHistory;

    /// <summary>Crea el controlador con sus casos de uso.</summary>
    /// <param name="processPrintRequest">Caso de uso de procesamiento de impresion.</param>
    /// <param name="getPrintHistory">Caso de uso de consulta de historial.</param>
    public PrintRequestsController(
        IProcessPrintRequestUseCase processPrintRequest,
        IGetPrintHistoryUseCase getPrintHistory)
    {
        _processPrintRequest = processPrintRequest;
        _getPrintHistory = getPrintHistory;
    }

    /// <summary>
    /// Procesa una solicitud de impresion sobre una ETQ/LPN pre-generada.
    /// </summary>
    /// <remarks>
    /// Un rechazo de negocio responde HTTP 200 con success=false y el motivo: la solicitud
    /// se proceso correctamente, lo que no procede es la impresion. Toda solicitud queda
    /// auditada, apruebe o rechace.
    ///
    /// Si la ETQ ya fue impresa antes, la solicitud se marca como reimpresion y exige
    /// motivo y rol Supervisor o Admin.
    /// </remarks>
    /// <param name="request">Solicitud con LPN, zona y motivo de reimpresion si aplica.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Resultado de impresion, aprobado o rechazado con su motivo.</returns>
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

    /// <summary>
    /// Consulta el historial de impresiones y reimpresiones.
    /// </summary>
    /// <remarks>
    /// El alcance depende del rol: un Operario recibe unicamente sus propias solicitudes
    /// y el filtro se impone en el servidor, mientras que Supervisor y Admin consultan
    /// la operacion completa. El campo meta.scope indica cual alcance se aplico.
    /// </remarks>
    /// <param name="filter">Filtros de busqueda y paginacion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de registros de auditoria ordenada por fecha descendente.</returns>
    // El historial es una consulta, no una transaccion: usa el limite de lectura y no

    // el mas estricto del endpoint de impresion.

    [EnableRateLimiting(RateLimitingSetup.QueryPolicy)]

    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<PrintHistoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistoryAsync(
        [FromQuery] PrintHistoryFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _getPrintHistory.ExecuteAsync(filter, cancellationToken);
        return Ok(result);
    }
}
