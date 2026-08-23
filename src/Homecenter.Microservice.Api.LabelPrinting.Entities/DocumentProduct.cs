namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Producto asociado al documento y, por extension, a sus etiquetas.
/// Una ETQ/LPN puede arrastrar varios productos: por eso la validacion de inventario
/// debe cumplirse para todos, no solo para el primero.
/// </summary>
public class DocumentProduct : EntityBase
{
    public int IdDocument { get; set; }

    public int IdProduct { get; set; }

    public decimal RequestedQty { get; set; }

    public string Uom { get; set; } = string.Empty;

    public Document Document { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
