using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Repositories;

/// <summary>
/// Persistencia y consulta de la auditoria de impresiones sobre PostgreSQL.
/// </summary>
public sealed class PrintRequestRepository : IPrintRequestRepository
{
    private readonly LabelPrintingDbContext _context;

    /// <summary>Crea el repositorio con el contexto de persistencia.</summary>
    /// <param name="context">Contexto de Entity Framework.</param>
    public PrintRequestRepository(LabelPrintingDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(PrintRequest request, CancellationToken cancellationToken = default)
    {
        _context.PrintRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> HasApprovedPrintAsync(string lpnId, CancellationToken cancellationToken = default) =>
        // Solo cuentan las impresiones aprobadas: un rechazo previo no convierte
        // el siguiente intento en reimpresion, porque nunca se imprimio nada.
        _context.PrintRequests
                .AsNoTracking()
                .AnyAsync(x => x.LpnId == lpnId && x.Result == PrintResult.Approved, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<PrintHistoryItemDto>> GetHistoryAsync(
        PrintHistoryFilterDto filter,
        int? restrictToUserId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PrintRequests
                            .Include(x => x.Zone)
                            .Include(x => x.User)
                            .AsNoTracking()
                            .AsQueryable();

        // La restriccion por usuario la impone el caso de uso segun el rol y se aplica
        // aqui, sobre el query: un operario no puede ampliarla manipulando el request.
        if (restrictToUserId.HasValue)
        {
            query = query.Where(x => x.IdUser == restrictToUserId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(filter.UserName))
        {
            query = query.Where(x => x.User.UserName == filter.UserName);
        }

        if (!string.IsNullOrWhiteSpace(filter.Lpn))
        {
            query = query.Where(x => x.LpnId == filter.Lpn);
        }

        if (!string.IsNullOrWhiteSpace(filter.ZoneCode))
        {
            query = query.Where(x => x.Zone != null && x.Zone.Code == filter.ZoneCode);
        }

        if (Enum.TryParse<PrintResult>(filter.Result, ignoreCase: true, out var result))
        {
            query = query.Where(x => x.Result == result);
        }

        if (Enum.TryParse<PrintEventType>(filter.EventType, ignoreCase: true, out var eventType))
        {
            query = query.Where(x => x.EventType == eventType);
        }

        if (filter.DateFrom.HasValue)
        {
            var from = filter.DateFrom.Value.ToUniversalTime();
            query = query.Where(x => x.ProcessedAt >= from);
        }

        if (filter.DateTo.HasValue)
        {
            var to = filter.DateTo.Value.ToUniversalTime();
            query = query.Where(x => x.ProcessedAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = Math.Clamp(filter.PageSize, 1, PrintHistoryFilterDto.MaxPageSize);

        var items = await query
            .OrderByDescending(x => x.ProcessedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PrintHistoryItemDto
            {
                Id = x.Id,
                CorrelationId = x.CorrelationId,
                EtqId = x.EtqId,
                LpnId = x.LpnId,
                ZoneCode = x.Zone != null ? x.Zone.Code : null,
                UserName = x.User.UserName,
                DocumentNumber = x.DocumentNumber,
                Result = x.Result == PrintResult.Approved ? "APPROVED" : "REJECTED",
                EventType = x.EventType == PrintEventType.Reprint ? "REPRINT" : "PRINT",
                RejectionCode = x.RejectionCode,
                RejectionMessage = x.RejectionMessage,
                ReprintReason = x.ReprintReason,
                ProcessedAt = x.ProcessedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PrintHistoryItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        // Se agrupa del lado de la base en lugar de traer las filas y contarlas en
        // memoria: el historial crece sin techo con la operacion diaria.
        var totals = await _context.PrintRequests
            .AsNoTracking()
            .GroupBy(x => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Approved = g.Count(x => x.Result == PrintResult.Approved),
                Rejected = g.Count(x => x.Result == PrintResult.Rejected),
                Reprints = g.Count(x => x.EventType == PrintEventType.Reprint)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var rejections = await _context.PrintRequests
            .AsNoTracking()
            .Where(x => x.RejectionCode != null)
            .GroupBy(x => x.RejectionCode!)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto
        {
            TotalRequests = totals?.Total ?? 0,
            Approved = totals?.Approved ?? 0,
            Rejected = totals?.Rejected ?? 0,
            Reprints = totals?.Reprints ?? 0,
            RejectionsByCode = rejections.ToDictionary(x => x.Code, x => x.Count)
        };
    }
}
