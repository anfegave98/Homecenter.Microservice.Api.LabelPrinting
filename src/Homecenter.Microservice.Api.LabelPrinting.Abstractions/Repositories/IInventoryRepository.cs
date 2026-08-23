using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

public interface IInventoryRepository
{
    /// <summary>
    /// Disponibilidad de varios productos en una zona, en una sola consulta.
    /// Se pide en lote a proposito: recorrer producto por producto generaria N+1.
    /// </summary>
    Task<IReadOnlyCollection<InventoryAvailability>> GetByProductsAndZoneAsync(
        IReadOnlyCollection<int> productIds,
        int zoneId,
        CancellationToken cancellationToken = default);
}
