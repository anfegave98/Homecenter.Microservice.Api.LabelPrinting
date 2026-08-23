using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

/// <summary>
/// Simulacion del envio a impresora: confirma logicamente el evento y, segun
/// configuracion, deja un archivo .zpl como evidencia de salida.
/// </summary>
public interface IPrintSimulator
{
    /// <summary>Ejecuta la impresion simulada de una etiqueta.</summary>
    /// <param name="label">Etiqueta pre-generada a imprimir.</param>
    /// <param name="correlationId">Llave de rastreo del caso.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Contenido ZPL enviado a impresion.</returns>
    Task<string> PrintAsync(Label label, Guid correlationId, CancellationToken cancellationToken = default);
}
