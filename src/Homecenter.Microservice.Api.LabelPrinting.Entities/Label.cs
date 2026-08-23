namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Etiqueta pre-generada. La ETQ ya existe antes de que llegue la solicitud:
/// su generacion esta fuera del alcance de esta solucion.
/// LpnId es la llave funcional de entrada del servicio.
/// </summary>
public class Label : EntityBase
{
    /// <summary>Identificador del documento origen.</summary>
    public int IdDocument { get; set; }

    /// <summary>Identificador de la etiqueta. Unico.</summary>
    public string EtqId { get; set; } = string.Empty;

    /// <summary>Identificador de la unidad logistica (License Plate Number). Unico.</summary>
    public string LpnId { get; set; } = string.Empty;

    /// <summary>Confirma que la etiqueta se genero previamente por el proceso de olas.</summary>
    public bool IsPreGenerated { get; set; } = true;

    /// <summary>Codigo de la plantilla de impresion aplicada.</summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>Contenido ZPL listo para enviar a la impresora.</summary>
    public string Zpl { get; set; } = string.Empty;

    /// <summary>Documento origen asociado.</summary>
    public Document Document { get; set; } = null!;
}
