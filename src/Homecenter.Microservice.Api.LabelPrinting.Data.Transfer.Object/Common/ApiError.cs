namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

/// <summary>
/// Error controlado expuesto al consumidor. Nunca transporta stack trace ni detalle de infraestructura.
/// </summary>
public sealed class ApiError
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    /// <summary>
    /// Detalle granular del rechazo. Para la regla de inventario transporta el listado
    /// de productos que incumplen, con su cantidad solicitada y disponible.
    /// </summary>
    public IReadOnlyCollection<object>? Details { get; init; }
}
