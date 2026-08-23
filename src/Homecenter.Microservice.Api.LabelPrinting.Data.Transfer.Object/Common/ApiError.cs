namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

/// <summary>
/// Error controlado expuesto al consumidor. Nunca transporta stack trace ni detalle
/// de infraestructura.
/// </summary>
public sealed class ApiError
{
    /// <summary>Codigo estable del error. Es contrato: el frontend decide con el.</summary>
    public required string Code { get; init; }

    /// <summary>Mensaje legible para el usuario operativo.</summary>
    public required string Message { get; init; }

    /// <summary>
    /// Detalle granular del rechazo. Para la regla de inventario transporta el listado
    /// de productos que incumplen, con su cantidad solicitada y disponible.
    /// </summary>
    public IReadOnlyCollection<object>? Details { get; init; }
}
