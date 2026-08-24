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
    private readonly IResolveReprintApprovalUseCase _resolveReprintApproval;
    private readonly IDownloadLabelUseCase _downloadLabel;

    /// <summary>Crea el controlador con sus casos de uso.</summary>
    /// <param name="processPrintRequest">Caso de uso de procesamiento de impresion.</param>
    /// <param name="getPrintHistory">Caso de uso de consulta de historial.</param>
    /// <param name="resolveReprintApproval">Caso de uso de autorizacion de reimpresiones.</param>
    /// <param name="downloadLabel">Caso de uso de entrega de la etiqueta.</param>
    public PrintRequestsController(
        IProcessPrintRequestUseCase processPrintRequest,
        IGetPrintHistoryUseCase getPrintHistory,
        IResolveReprintApprovalUseCase resolveReprintApproval,
        IDownloadLabelUseCase downloadLabel)
    {
        _processPrintRequest = processPrintRequest;
        _getPrintHistory = getPrintHistory;
        _resolveReprintApproval = resolveReprintApproval;
        _downloadLabel = downloadLabel;
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
    /// motivo. Un Supervisor o Admin la ejecuta de inmediato; cualquier otro rol la deja
    /// en estado PENDING_APPROVAL para que un autorizado la resuelva.
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

    /// <summary>
    /// Consulta las reimpresiones que esperan autorizacion.
    /// </summary>
    /// <remarks>
    /// Es la bandeja de trabajo del Supervisor. Se ordena de la mas antigua a la mas
    /// reciente: quien lleva mas tiempo esperando se atiende primero.
    /// </remarks>
    /// <param name="page">Pagina solicitada, base 1.</param>
    /// <param name="pageSize">Registros por pagina.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de solicitudes pendientes.</returns>
    [EnableRateLimiting(RateLimitingSetup.QueryPolicy)]
    [HttpGet("pending")]
    [Authorize(Roles = $"{RoleName.Supervisor},{RoleName.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<PrintHistoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _resolveReprintApproval.GetPendingAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Autoriza una reimpresion pendiente.
    /// </summary>
    /// <remarks>
    /// La autorizacion no imprime a ciegas: las reglas se vuelven a evaluar con los datos
    /// del momento de la decision. Si el documento se anulo o el inventario se agoto
    /// mientras la solicitud esperaba, la respuesta es un rechazo con ese motivo y no
    /// con el visto bueno.
    /// </remarks>
    /// <param name="id">Identificador de la solicitud pendiente.</param>
    /// <param name="decision">Comentario del autorizador.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Resultado final de la solicitud.</returns>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = $"{RoleName.Supervisor},{RoleName.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<PrintResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveAsync(
        int id,
        [FromBody] ReprintDecisionDto decision,
        CancellationToken cancellationToken)
    {
        var result = await _resolveReprintApproval.ApproveAsync(id, decision, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Niega una reimpresion pendiente.
    /// </summary>
    /// <remarks>
    /// El motivo es obligatorio: sin el, el operario no sabe que corregir y soporte no
    /// tiene rastro de por que se nego el duplicado.
    /// </remarks>
    /// <param name="id">Identificador de la solicitud pendiente.</param>
    /// <param name="decision">Motivo del rechazo.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Resultado final de la solicitud.</returns>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = $"{RoleName.Supervisor},{RoleName.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<PrintResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectAsync(
        int id,
        [FromBody] ReprintDecisionDto decision,
        CancellationToken cancellationToken)
    {
        var result = await _resolveReprintApproval.RejectAsync(id, decision, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Descarga la etiqueta de una solicitud aprobada.
    /// </summary>
    /// <remarks>
    /// Es la materialización de la impresión simulada: la confirmación lógica ya ocurrió
    /// al procesar la solicitud, y esto entrega el archivo ZPL.
    ///
    /// Una solicitud aprobada da derecho a **una** descarga. Volver a necesitar la
    /// etiqueta es una reimpresión, con su motivo y su autorización.
    ///
    /// Cuando procede, la respuesta es el archivo y no el envelope. Cuando no procede sí
    /// se responde el envelope, porque ahí hay un motivo que comunicar.
    /// </remarks>
    /// <param name="id">Identificador de la solicitud aprobada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El archivo .zpl, o el motivo por el cual no se entrega.</returns>
    [EnableRateLimiting(RateLimitingSetup.QueryPolicy)]
    [HttpGet("{id:int}/label")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LabelDownloadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadLabelAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _downloadLabel.ExecuteAsync(id, cancellationToken);

        if (!result.Success)
        {
            return Ok(result);
        }

        var label = result.Data!;

        return File(
            System.Text.Encoding.UTF8.GetBytes(label.Content),
            "application/vnd.zebra.zpl",
            label.FileName);
    }
}
