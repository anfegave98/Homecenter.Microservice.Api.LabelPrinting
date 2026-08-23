using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Repositories;

public sealed class LabelRepository : ILabelRepository
{
    private readonly LabelPrintingDbContext _context;

    public LabelRepository(LabelPrintingDbContext context)
    {
        _context = context;
    }

    public Task<Label?> GetByLpnOrEtqAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult<Label?>(null);
        }

        var normalized = key.Trim();

        // Se acepta LPN o ETQ como llave de entrada: el enunciado admite ambas
        // y el operario no siempre sabe cual tiene a la mano.
        return _context.Labels
                       .Include(x => x.Document).ThenInclude(x => x.Zone)
                       .Include(x => x.Document).ThenInclude(x => x.DocumentProducts).ThenInclude(x => x.Product)
                       .AsNoTracking()
                       .FirstOrDefaultAsync(
                           x => x.State && (x.LpnId == normalized || x.EtqId == normalized),
                           cancellationToken);
    }
}
