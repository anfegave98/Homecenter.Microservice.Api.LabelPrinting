using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Repositories;

/// <summary>Componente ZoneRepository del submodulo de impresion.</summary>
public sealed class ZoneRepository : IZoneRepository
{
    private readonly LabelPrintingDbContext _context;

    /// <summary>Crea una instancia con sus dependencias.</summary>
    public ZoneRepository(LabelPrintingDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Zone>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Zones
                      .Where(x => x.State)
                      .OrderBy(x => x.Code)
                      .AsNoTracking()
                      .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Zone?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.Zones.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && x.State, cancellationToken);
}
