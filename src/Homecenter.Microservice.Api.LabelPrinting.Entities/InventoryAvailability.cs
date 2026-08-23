namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Disponibilidad de un producto en una zona. Fuente de la Regla 3.
/// Son dos condiciones distintas y ambas deben cumplirse: tener cantidad suficiente
/// (AvailableQty) y estar abastecido en la zona (IsStocked). Un producto puede tener
/// stock registrado y aun asi no estar abastecido para operar.
/// </summary>
public class InventoryAvailability : EntityBase
{
    public int IdProduct { get; set; }

    public int IdZone { get; set; }

    public decimal AvailableQty { get; set; }

    public bool IsStocked { get; set; }

    public DateTimeOffset LastUpdateDate { get; set; } = DateTimeOffset.UtcNow;

    public Product Product { get; set; } = null!;

    public Zone Zone { get; set; } = null!;
}
