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
    public Task<PrintRequest?> GetPendingByIdAsync(int id, CancellationToken cancellationToken = default) =>
        // Se filtra por estado y no solo por identificador: si otro supervisor ya la
        // resolvio, esta solicitud dejo de estar disponible para decidir.
        _context.PrintRequests
                .Include(x => x.User)
                .Include(x => x.Zone)
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.Result == PrintResult.PendingApproval,
                    cancellationToken);

    /// <inheritdoc />
    public async Task UpdateAsync(PrintRequest request, CancellationToken cancellationToken = default)
    {
        _context.PrintRequests.Update(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<PrintHistoryItemDto>> GetPendingApprovalsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PrintRequests
                            .Include(x => x.Zone)
                            .Include(x => x.User)
                            .AsNoTracking()
                            .Where(x => x.Result == PrintResult.PendingApproval);

        var total = await query.CountAsync(cancellationToken);

        var safePage = page < 1 ? 1 : page;
        var safePageSize = Math.Clamp(pageSize, 1, PrintHistoryFilterDto.MaxPageSize);

        var items = await query
            // Ascendente, al reves que el historial: una bandeja de trabajo se atiende
            // por antiguedad, y el operario que lleva mas tiempo esperando va primero.
            .OrderBy(x => x.ProcessedAt)
            .ThenBy(x => x.Id)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(HistoryProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<PrintHistoryItemDto>
        {
            Items = items,
            Total = total,
            Page = safePage,
            PageSize = safePageSize
        };
    }

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

        // El contrato publico usa PENDING_APPROVAL y el enum PendingApproval: se quita
        // el separador para que el filtro acepte el literal que el cliente ya conoce.
        var requestedResult = filter.Result?.Replace("_", string.Empty, StringComparison.Ordinal);

        if (Enum.TryParse<PrintResult>(requestedResult, ignoreCase: true, out var result))
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
            .Select(HistoryProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<PrintHistoryItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Proyeccion unica de la fila de auditoria.
    ///
    /// Es una expresion y no un metodo para que EF pueda traducirla a SQL: si fuera
    /// una llamada, la consulta se evaluaria en memoria despues de traer las entidades
    /// completas.
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<PrintRequest, PrintHistoryItemDto>> HistoryProjection =>
        x => new PrintHistoryItemDto
        {
            Id = x.Id,
            CorrelationId = x.CorrelationId,
            EtqId = x.EtqId,
            LpnId = x.LpnId,
            ZoneCode = x.Zone != null ? x.Zone.Code : null,
            UserName = x.User.UserName,
            DocumentNumber = x.DocumentNumber,
            Result = x.Result == PrintResult.Approved
                ? "APPROVED"
                : x.Result == PrintResult.PendingApproval ? "PENDING_APPROVAL" : "REJECTED",
            EventType = x.EventType == PrintEventType.Reprint ? "REPRINT" : "PRINT",
            RejectionCode = x.RejectionCode,
            RejectionMessage = x.RejectionMessage,
            ReprintReason = x.ReprintReason,
            ProcessedAt = x.ProcessedAt,
            ApprovedBy = x.Approver != null ? x.Approver.UserName : null,
            DecidedAt = x.DecidedAt,
            ApprovalNote = x.ApprovalNote
        };

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
                Reprints = g.Count(x => x.EventType == PrintEventType.Reprint),
                PendingApproval = g.Count(x => x.Result == PrintResult.PendingApproval)
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
            PendingApproval = totals?.PendingApproval ?? 0,
            RejectionsByCode = rejections.ToDictionary(x => x.Code, x => x.Count)
        };
    }
}
