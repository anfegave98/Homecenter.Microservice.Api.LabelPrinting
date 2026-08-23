namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Detalle del incumplimiento por producto. Viaja en error.details para que el
/// operario sepa exactamente que producto bloquea la impresion: un rechazo generico
/// obliga a adivinar y no cumple el criterio de aceptacion de la HU-02.
/// </summary>
public sealed class InventoryShortageDto
{
    /// <summary>Codigo del articulo que incumple.</summary>
    public required string ProductCode { get; init; }

    /// <summary>Descripcion del articulo que incumple.</summary>
    public required string ProductDescription { get; init; }

    /// <summary>Cantidad exigida por el documento.</summary>
    public required decimal RequestedQty { get; init; }

    /// <summary>Cantidad realmente disponible en la zona.</summary>
    public required decimal AvailableQty { get; init; }

    /// <summary>Indicador de abastecimiento en la zona.</summary>
    public required bool IsStocked { get; init; }

    /// <summary>Explicacion legible de por que este producto bloquea la impresion.</summary>
    public required string Reason { get; init; }
}
