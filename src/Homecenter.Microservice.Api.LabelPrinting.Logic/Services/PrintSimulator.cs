using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Services;

/// <summary>
/// Simula el envio a impresora.
///
/// El alcance de la prueba no contempla hardware real: la impresion se confirma como
/// evento logico y, si la configuracion lo pide, se deja el archivo .zpl como evidencia
/// verificable de la salida. Un fallo al escribir el archivo NO invalida la impresion:
/// la decision de negocio ya se tomo y la evidencia es un accesorio, no la operacion.
/// </summary>
public sealed class PrintSimulator : IPrintSimulator
{
    private readonly PrintingOptions _options;
    private readonly ILogger<PrintSimulator> _logger;

    /// <summary>Crea una instancia con sus dependencias.</summary>
    public PrintSimulator(IOptions<PrintingOptions> options, ILogger<PrintSimulator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> PrintAsync(Label label, Guid correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Impresion simulada. CorrelationId={CorrelationId} Etq={EtqId} Lpn={LpnId} Plantilla={TemplateCode}",
            correlationId,
            label.EtqId,
            label.LpnId,
            label.TemplateCode);

        if (_options.PersistZplFile)
        {
            await TryPersistZplAsync(label, correlationId, cancellationToken);
        }

        return label.Zpl;
    }

    private async Task TryPersistZplAsync(Label label, Guid correlationId, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(_options.OutputDirectory);

            var fileName = $"{label.LpnId}_{correlationId:N}.zpl";
            var fullPath = Path.Combine(_options.OutputDirectory, fileName);

            await File.WriteAllTextAsync(fullPath, label.Zpl, cancellationToken);

            _logger.LogInformation("Archivo ZPL generado: {Path}", fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // En Render el sistema de archivos es efimero y puede ser de solo lectura.
            // Se registra y se continua: la impresion ya fue confirmada.
            _logger.LogWarning(ex, "No se pudo escribir el archivo ZPL. La impresion se mantiene confirmada.");
        }
    }
}
