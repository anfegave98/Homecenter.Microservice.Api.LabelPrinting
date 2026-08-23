using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

/// <summary>
/// Simulacion del envio a impresora: confirma logicamente el evento y, segun
/// configuracion, deja un archivo .zpl como evidencia de salida.
/// </summary>
public interface IPrintSimulator
{
    Task<string> PrintAsync(Label label, Guid correlationId, CancellationToken cancellationToken = default);
}
