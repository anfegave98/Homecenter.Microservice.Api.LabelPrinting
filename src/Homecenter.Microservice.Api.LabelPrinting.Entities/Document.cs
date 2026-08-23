using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Documento origen del proceso (nota pedido / orden).
/// Su estado es insumo directo de la Regla 2: ANULADA o DEVUELTA bloquean la impresion.
/// </summary>
public class Document : EntityBase
{
    /// <summary>Identificador transaccional de la solicitud que origino el documento.</summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Tipo de documento, por ejemplo NOTA_PEDIDO.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Numero del documento. Debe ser unico.</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Estado del documento. Determina si admite impresion.</summary>
    public DocumentStatus Status { get; set; }

    /// <summary>Identificador de la zona logistica del documento.</summary>
    public int IdZone { get; set; }

    /// <summary>Usuario que genero el documento en el sistema origen.</summary>
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>Fecha y hora de la solicitud original, en UTC.</summary>
    public DateTimeOffset RequestDateTime { get; set; }

    /// <summary>Zona logistica asociada.</summary>
    public Zone Zone { get; set; } = null!;

    /// <summary>Etiquetas pre-generadas del documento.</summary>
    public ICollection<Label> Labels { get; set; } = new List<Label>();

    /// <summary>Productos asociados al documento con su cantidad solicitada.</summary>
    public ICollection<DocumentProduct> DocumentProducts { get; set; } = new List<DocumentProduct>();
}
