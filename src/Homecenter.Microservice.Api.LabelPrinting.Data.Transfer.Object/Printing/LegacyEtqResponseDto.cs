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
    /// <summary>Identificador de la etiqueta.</summary>
    public required string IdEtiqueta { get; init; }

    /// <summary>Documento origen, expuesto con el nombre que usa el contrato legacy.</summary>
    public required string PurchaseOrder { get; init; }

    /// <summary>Identificador transaccional de la solicitud origen.</summary>
    public required string TcOrderId { get; init; }

    /// <summary>Codigo del primer producto de la ETQ.</summary>
    public required string Sku { get; init; }

    /// <summary>Cantidad solicitada del primer producto.</summary>
    public required decimal Unidades { get; init; }

    /// <summary>Contenido ZPL de la etiqueta.</summary>
    public required string Zpl { get; init; }

    /// <summary>
    /// True cuando la ETQ arrastra mas de un producto y este bloque solo alcanza
    /// a representar el primero.
    /// </summary>
    public required bool HasMultipleProducts { get; init; }
}
