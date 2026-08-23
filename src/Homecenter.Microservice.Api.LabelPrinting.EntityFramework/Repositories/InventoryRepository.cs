using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly LabelPrintingDbContext _context;

    public InventoryRepository(LabelPrintingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<InventoryAvailability>> GetByProductsAndZoneAsync(
        IReadOnlyCollection<int> productIds,
        int zoneId,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return Array.Empty<InventoryAvailability>();
        }

        return await _context.InventoryAvailability
                             .Where(x => x.State && x.IdZone == zoneId && productIds.Contains(x.IdProduct))
                             .AsNoTracking()
                             .ToListAsync(cancellationToken);
    }
}
