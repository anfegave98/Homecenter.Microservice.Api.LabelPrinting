using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Repositories;

public sealed class ZoneRepository : IZoneRepository
{
    private readonly LabelPrintingDbContext _context;

    public ZoneRepository(LabelPrintingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Zone>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Zones
                      .Where(x => x.State)
                      .OrderBy(x => x.Code)
                      .AsNoTracking()
                      .ToListAsync(cancellationToken);

    public Task<Zone?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.Zones.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && x.State, cancellationToken);
}
