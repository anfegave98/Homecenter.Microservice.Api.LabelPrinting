namespace Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

/// <summary>
/// Naturaleza del evento: primera impresion o reimpresion de una ETQ/LPN.
/// </summary>
public enum PrintEventType
{
    /// <summary>Primera impresion de la etiqueta.</summary>
    Print = 1,

    /// <summary>La etiqueta ya habia sido impresa: requiere motivo y rol autorizado.</summary>
    Reprint = 2
}
