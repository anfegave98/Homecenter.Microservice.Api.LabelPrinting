namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Etiqueta pre-generada. La ETQ ya existe antes de que llegue la solicitud:
/// su generacion esta fuera del alcance de esta solucion.
/// LpnId es la llave funcional de entrada del servicio.
/// </summary>
public class Label : EntityBase
{
    public int IdDocument { get; set; }

    public string EtqId { get; set; } = string.Empty;

    public string LpnId { get; set; } = string.Empty;

    public bool IsPreGenerated { get; set; } = true;

    public string TemplateCode { get; set; } = string.Empty;

    public string Zpl { get; set; } = string.Empty;

    public Document Document { get; set; } = null!;
}
