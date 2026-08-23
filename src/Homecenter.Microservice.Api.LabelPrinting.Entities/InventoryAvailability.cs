namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Disponibilidad de un producto en una zona. Fuente de la Regla 3.
/// Son dos condiciones distintas y ambas deben cumplirse: tener cantidad suficiente
/// (AvailableQty) y estar abastecido en la zona (IsStocked). Un producto puede tener
/// stock registrado y aun asi no estar abastecido para operar.
/// </summary>
public class InventoryAvailability : EntityBase
{
    /// <summary>Identificador del producto.</summary>
    public int IdProduct { get; set; }

    /// <summary>Identificador de la zona logistica.</summary>
    public int IdZone { get; set; }

    /// <summary>Cantidad disponible del producto en la zona.</summary>
    public decimal AvailableQty { get; set; }

    /// <summary>Indica si el producto esta abastecido para operar en la zona.</summary>
    public bool IsStocked { get; set; }

    /// <summary>Fecha de la ultima actualizacion del inventario, en UTC.</summary>
    public DateTimeOffset LastUpdateDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Producto asociado.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Zona asociada.</summary>
    public Zone Zone { get; set; } = null!;
}
