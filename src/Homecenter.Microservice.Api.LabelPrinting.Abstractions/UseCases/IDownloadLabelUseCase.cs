using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

/// <summary>
/// Entrega de la etiqueta de una solicitud aprobada.
/// </summary>
public interface IDownloadLabelUseCase
{
    /// <summary>
    /// Entrega el ZPL de una solicitud aprobada y deja constancia de la descarga.
    /// </summary>
    /// <param name="requestId">Solicitud aprobada de la que se quiere la etiqueta.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La etiqueta lista para descargar, o el motivo por el cual no procede.</returns>
    Task<ApiResponse<LabelDownloadDto>> ExecuteAsync(
        int requestId,
        CancellationToken cancellationToken = default);
}
