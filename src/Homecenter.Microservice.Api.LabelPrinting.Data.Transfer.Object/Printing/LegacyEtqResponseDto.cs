namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Bloque compatible con el anexo responseEtq.json.
///
/// Ese contrato expone un unico SKU escalar, pero el enunciado exige que una ETQ/LPN
/// pueda cargar varios productos. Se conserva la forma original tomando el primer
/// producto y se marca HasMultipleProducts para que el consumidor sepa que esta viendo
/// una vista parcial: degradar de forma declarada es aceptable, perder productos en
/// silencio no lo es.
/// </summary>
public sealed class LegacyEtqResponseDto
{
    public required string IdEtiqueta { get; init; }

    public required string PurchaseOrder { get; init; }

    public required string TcOrderId { get; init; }

    public required string Sku { get; init; }

    public required decimal Unidades { get; init; }

    public required string Zpl { get; init; }

    public required bool HasMultipleProducts { get; init; }
}
