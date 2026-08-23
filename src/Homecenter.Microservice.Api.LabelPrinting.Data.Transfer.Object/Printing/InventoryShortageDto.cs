namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Detalle del incumplimiento por producto. Viaja en error.details para que el
/// operario sepa exactamente que producto bloquea la impresion: un rechazo generico
/// obliga a adivinar y no cumple el criterio de aceptacion de la HU-02.
/// </summary>
public sealed class InventoryShortageDto
{
    public required string ProductCode { get; init; }

    public required string ProductDescription { get; init; }

    public required decimal RequestedQty { get; init; }

    public required decimal AvailableQty { get; init; }

    public required bool IsStocked { get; init; }

    public required string Reason { get; init; }
}
