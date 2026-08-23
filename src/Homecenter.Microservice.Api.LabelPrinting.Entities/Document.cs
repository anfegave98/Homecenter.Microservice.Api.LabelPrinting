using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Documento origen del proceso (nota pedido / orden).
/// Su estado es insumo directo de la Regla 2: ANULADA o DEVUELTA bloquean la impresion.
/// </summary>
public class Document : EntityBase
{
    public string RequestId { get; set; } = string.Empty;

    public string DocumentType { get; set; } = string.Empty;

    public string DocumentNumber { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; }

    public int IdZone { get; set; }

    public string RequestedBy { get; set; } = string.Empty;

    public DateTimeOffset RequestDateTime { get; set; }

    public Zone Zone { get; set; } = null!;

    public ICollection<Label> Labels { get; set; } = new List<Label>();

    public ICollection<DocumentProduct> DocumentProducts { get; set; } = new List<DocumentProduct>();
}
