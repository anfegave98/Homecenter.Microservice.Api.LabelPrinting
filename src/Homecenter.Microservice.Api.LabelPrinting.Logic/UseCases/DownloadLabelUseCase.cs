using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;

/// <summary>
/// Entrega la etiqueta de una solicitud aprobada.
///
/// Es la materializacion de la impresion simulada: el enunciado admite confirmar el
/// evento, generar un archivo de salida o ambas cosas. La confirmacion logica ya ocurre
/// al procesar la solicitud; esto pone el archivo en manos del operario.
///
/// Una solicitud aprobada da derecho a UNA descarga. La etiqueta fisica sale de la
/// impresora una sola vez, y permitir bajarla indefinidamente convertiria el control de
/// reimpresiones en un tramite decorativo: cualquiera obtendria copias sin motivo ni
/// autorizacion, que es justo lo que la Regla 4 existe para impedir.
/// </summary>
public sealed class DownloadLabelUseCase : IDownloadLabelUseCase
{
    private readonly IPrintRequestRepository _printRequestRepository;
    private readonly ILabelRepository _labelRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<DownloadLabelUseCase> _logger;

    /// <summary>Crea el caso de uso con sus dependencias.</summary>
    /// <param name="printRequestRepository">Acceso a la auditoria de impresiones.</param>
    /// <param name="labelRepository">Acceso a las etiquetas pre-generadas.</param>
    /// <param name="currentUser">Identidad tomada del token.</param>
    /// <param name="logger">Registro de eventos.</param>
    public DownloadLabelUseCase(
        IPrintRequestRepository printRequestRepository,
        ILabelRepository labelRepository,
        ICurrentUserAccessor currentUser,
        ILogger<DownloadLabelUseCase> logger)
    {
        _printRequestRepository = printRequestRepository;
        _labelRepository = labelRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<LabelDownloadDto>> ExecuteAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        // Mismo criterio que el historial: el operario ve y descarga lo suyo, supervisor
        // y administrador la operacion completa. La restriccion se impone aqui y viaja
        // al query, no se comprueba sobre una fila ya leida.
        var seesEverything = _currentUser.IsInRole(RoleName.Supervisor)
                          || _currentUser.IsInRole(RoleName.Admin);

        var restrictToUserId = seesEverything ? null : _currentUser.UserId;

        var request = await _printRequestRepository.GetApprovedForDownloadAsync(
            requestId,
            restrictToUserId,
            cancellationToken);

        if (request is null)
        {
            // Un mismo motivo para "no existe", "no fue aprobada" y "es de otro usuario":
            // distinguirlos le confirmaria a un operario que la solicitud existe y que
            // pertenece a alguien mas.
            return ApiResponse<LabelDownloadDto>.Fail(
                RejectionCodes.LabelNotAvailable,
                $"No hay una etiqueta disponible para la solicitud {requestId}.");
        }

        if (request.DownloadedAt.HasValue)
        {
            return ApiResponse<LabelDownloadDto>.Fail(
                RejectionCodes.LabelAlreadyDownloaded,
                $"La etiqueta de la solicitud {requestId} ya se descargó el "
                + $"{request.DownloadedAt.Value:dd/MM/yyyy HH:mm} UTC. "
                + "Para obtener otra copia debe solicitarse una reimpresión.");
        }

        var label = await _labelRepository.GetByLpnOrEtqAsync(request.LpnId, cancellationToken);

        if (label is null)
        {
            // La solicitud se aprobo, luego la etiqueta existia. Si ya no aparece, el dato
            // cambio debajo y eso no es un rechazo de negocio que el operario pueda
            // corregir: se registra para que soporte lo vea.
            _logger.LogWarning(
                "Solicitud aprobada sin etiqueta resoluble. Solicitud={Id} Lpn={Lpn}",
                request.Id, request.LpnId);

            return ApiResponse<LabelDownloadDto>.Fail(
                RejectionCodes.LabelNotAvailable,
                $"No hay una etiqueta disponible para la solicitud {requestId}.");
        }

        request.DownloadedAt = DateTimeOffset.UtcNow;
        request.IdDownloadedBy = _currentUser.UserId;

        await _printRequestRepository.UpdateAsync(request, cancellationToken);

        _logger.LogInformation(
            "Etiqueta descargada. Solicitud={Id} Etq={Etq} Lpn={Lpn} Usuario={Usuario}",
            request.Id, label.EtqId, request.LpnId, _currentUser.UserName);

        return ApiResponse<LabelDownloadDto>.Ok(new LabelDownloadDto
        {
            Content = label.Zpl,
            // Se nombra por ETQ y solicitud: dos descargas de la misma etiqueta en dias
            // distintos no deben pisarse en la carpeta de descargas del operario.
            FileName = $"{label.EtqId}_{request.Id}.zpl"
        });
    }
}
