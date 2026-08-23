namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Producto asociado a la ETQ con su situacion de inventario en la zona consultada.
/// </summary>
public sealed class ProductAvailabilityDto
{
    /// <summary>Codigo del articulo (SKU).</summary>
    public required string ProductCode { get; init; }

    /// <summary>Descripcion comercial del articulo.</summary>
    public required string ProductDescription { get; init; }

    /// <summary>Cantidad que exige el documento origen.</summary>
    public required decimal RequestedQty { get; init; }

    /// <summary>Unidad de medida.</summary>
    public required string Uom { get; init; }

    /// <summary>Cantidad disponible en la zona.</summary>
    public required decimal AvailableQty { get; init; }

    /// <summary>Indica si el producto esta abastecido en la zona.</summary>
    public required bool IsStocked { get; init; }

    /// <summary>True solo si hay cantidad suficiente Y el producto esta abastecido.</summary>
    public required bool IsEligible { get; init; }
}
