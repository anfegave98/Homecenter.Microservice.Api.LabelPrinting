namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Producto asociado al documento y, por extension, a sus etiquetas.
/// Una ETQ/LPN puede arrastrar varios productos: por eso la validacion de inventario
/// debe cumplirse para todos, no solo para el primero.
/// </summary>
public class DocumentProduct : EntityBase
{
    /// <summary>Identificador del documento origen.</summary>
    public int IdDocument { get; set; }

    /// <summary>Identificador del producto.</summary>
    public int IdProduct { get; set; }

    /// <summary>Cantidad solicitada. Se compara contra la disponibilidad de la zona.</summary>
    public decimal RequestedQty { get; set; }

    /// <summary>Unidad de medida, por ejemplo UND, PAR o CAJ.</summary>
    public string Uom { get; set; } = string.Empty;

    /// <summary>Documento origen asociado.</summary>
    public Document Document { get; set; } = null!;

    /// <summary>Producto asociado.</summary>
    public Product Product { get; set; } = null!;
}
