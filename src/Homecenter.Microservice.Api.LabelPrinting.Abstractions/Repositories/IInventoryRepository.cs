using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

/// <summary>Consulta de disponibilidad de inventario por zona.</summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Disponibilidad de varios productos en una zona, en una sola consulta.
    /// Se pide en lote a proposito: recorrer producto por producto generaria N+1.
    /// </summary>
    /// <param name="productIds">Identificadores de los productos a consultar.</param>
    /// <param name="zoneId">Identificador de la zona.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Filas de disponibilidad encontradas para esa combinacion.</returns>
    Task<IReadOnlyCollection<InventoryAvailability>> GetByProductsAndZoneAsync(
        IReadOnlyCollection<int> productIds,
        int zoneId,
        CancellationToken cancellationToken = default);
}
