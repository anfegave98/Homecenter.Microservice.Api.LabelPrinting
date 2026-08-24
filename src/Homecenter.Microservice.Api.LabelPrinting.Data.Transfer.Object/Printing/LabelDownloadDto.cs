namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Etiqueta lista para entregar como archivo.
///
/// Es el único contrato del submódulo que no viaja dentro del envelope: el consumidor
/// recibe el archivo, no un JSON con el archivo adentro. El envelope sigue usándose
/// cuando la descarga no procede, porque ahí sí hay un motivo que comunicar.
/// </summary>
public sealed class LabelDownloadDto
{
    /// <summary>Contenido ZPL de la etiqueta.</summary>
    public required string Content { get; init; }

    /// <summary>Nombre sugerido del archivo, derivado de la ETQ y del caso.</summary>
    public required string FileName { get; init; }
}
