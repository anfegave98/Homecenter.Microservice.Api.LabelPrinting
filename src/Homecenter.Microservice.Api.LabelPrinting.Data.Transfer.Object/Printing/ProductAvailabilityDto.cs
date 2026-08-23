namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Producto asociado a la ETQ con su situacion de inventario en la zona consultada.
/// </summary>
public sealed class ProductAvailabilityDto
{
    public required string ProductCode { get; init; }

    public required string ProductDescription { get; init; }

    public required decimal RequestedQty { get; init; }

    public required string Uom { get; init; }

    public required decimal AvailableQty { get; init; }

    public required bool IsStocked { get; init; }

    /// <summary>True solo si hay cantidad suficiente Y el producto esta abastecido.</summary>
    public required bool IsEligible { get; init; }
}
