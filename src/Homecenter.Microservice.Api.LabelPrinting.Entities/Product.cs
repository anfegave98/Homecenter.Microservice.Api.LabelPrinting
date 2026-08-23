namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Articulo del catalogo. Corresponde al SKU: identifica un tipo de producto,
/// no una unidad fisica concreta.
/// </summary>
public class Product : EntityBase
{
    /// <summary>Codigo del articulo (SKU).</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Descripcion comercial del articulo.</summary>
    public string ProductDescription { get; set; } = string.Empty;
}
