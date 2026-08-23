using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Repositories;

public sealed class PrintRequestRepository : IPrintRequestRepository
{
    private readonly LabelPrintingDbContext _context;

    public PrintRequestRepository(LabelPrintingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PrintRequest request, CancellationToken cancellationToken = default)
    {
        _context.PrintRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasApprovedPrintAsync(string lpnId, CancellationToken cancellationToken = default) =>
        // Solo cuentan las impresiones aprobadas: un rechazo previo no convierte
        // el siguiente intento en reimpresion, porque nunca se imprimio nada.
        _context.PrintRequests
                .AsNoTracking()
                .AnyAsync(x => x.LpnId == lpnId && x.Result == PrintResult.Approved, cancellationToken);
}
