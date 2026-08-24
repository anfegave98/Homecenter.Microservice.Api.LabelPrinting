using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;

/// <summary>
/// Dobles en memoria de los repositorios y servicios que consume el caso de uso.
///
/// Son implementaciones explicitas y no mocks configurados por expresion: el caso de
/// uso hace pocas llamadas y bien definidas, y una clase legible deja mas claro que
/// escenario esta montado que una cadena de Setup().
/// </summary>
public sealed class InMemoryLabelRepository : ILabelRepository
{
    private readonly Label? _label;

    public InMemoryLabelRepository(Label? label) => _label = label;

    public Task<Label?> GetByLpnOrEtqAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_label is null)
        {
            return Task.FromResult<Label?>(null);
        }

        var matches = string.Equals(_label.LpnId, key, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(_label.EtqId, key, StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(matches ? _label : null);
    }
}

/// <summary>Catalogo de zonas en memoria.</summary>
public sealed class InMemoryZoneRepository : IZoneRepository
{
    private readonly IReadOnlyCollection<Zone> _zones;

    public InMemoryZoneRepository(params Zone[] zones) => _zones = zones;

    public Task<IReadOnlyCollection<Zone>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_zones);

    public Task<Zone?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_zones.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));
}

/// <summary>Disponibilidad en memoria, indexada por producto y zona.</summary>
public sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly IReadOnlyCollection<InventoryAvailability> _rows;

    public InMemoryInventoryRepository(params InventoryAvailability[] rows) => _rows = rows;

    public Task<IReadOnlyCollection<InventoryAvailability>> GetByProductsAndZoneAsync(
        IReadOnlyCollection<int> productIds,
        int zoneId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<InventoryAvailability> result = _rows
            .Where(x => x.IdZone == zoneId && productIds.Contains(x.IdProduct))
            .ToArray();

        return Task.FromResult(result);
    }
}

/// <summary>
/// Auditoria en memoria. Conserva lo persistido para poder afirmar que TODA
/// solicitud queda registrada, tanto la aprobada como la rechazada.
/// </summary>
public sealed class InMemoryPrintRequestRepository : IPrintRequestRepository
{
    private readonly bool _hasApprovedPrint;

    public InMemoryPrintRequestRepository(bool hasApprovedPrint = false) => _hasApprovedPrint = hasApprovedPrint;

    /// <summary>Solicitudes persistidas durante la prueba.</summary>
    public List<PrintRequest> Saved { get; } = new();

    public Task AddAsync(PrintRequest request, CancellationToken cancellationToken = default)
    {
        Saved.Add(request);
        return Task.CompletedTask;
    }

    public Task<bool> HasApprovedPrintAsync(string lpnId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_hasApprovedPrint);

    public Task<PagedResult<PrintHistoryItemDto>> GetHistoryAsync(
        PrintHistoryFilterDto filter,
        int? restrictToUserId,
        CancellationToken cancellationToken = default)
    {
        LastRestrictToUserId = restrictToUserId;
        HistoryWasQueried = true;

        return Task.FromResult(new PagedResult<PrintHistoryItemDto>
        {
            Items = Array.Empty<PrintHistoryItemDto>(),
            Total = 0,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    public Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new AdminDashboardDto
        {
            TotalRequests = 0,
            Approved = 0,
            Rejected = 0,
            Reprints = 0,
            PendingApproval = 0,
            RejectionsByCode = new Dictionary<string, int>()
        });

    /// <summary>Solicitudes que el doble entrega como pendientes de autorizacion.</summary>
    public List<PrintRequest> Pending { get; } = new();

    /// <summary>Solicitudes actualizadas tras una decision del autorizador.</summary>
    public List<PrintRequest> Updated { get; } = new();

    public Task<PagedResult<PrintHistoryItemDto>> GetPendingApprovalsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<PrintHistoryItemDto>
        {
            Items = Array.Empty<PrintHistoryItemDto>(),
            Total = Pending.Count,
            Page = page,
            PageSize = pageSize
        });

    public Task<PrintRequest?> GetPendingByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Pending.FirstOrDefault(x => x.Id == id));

    public Task UpdateAsync(PrintRequest request, CancellationToken cancellationToken = default)
    {
        Updated.Add(request);
        return Task.CompletedTask;
    }

    /// <summary>Restriccion por usuario con la que el caso de uso llamo al repositorio.</summary>
    public int? LastRestrictToUserId { get; private set; }

    /// <summary>True si el historial llego a consultarse.</summary>
    public bool HistoryWasQueried { get; private set; }
}

/// <summary>Identidad autenticada fija para la prueba.</summary>
public sealed class StubCurrentUserAccessor : ICurrentUserAccessor
{
    public StubCurrentUserAccessor(int? userId, string? userName, params string[] roles)
    {
        UserId = userId;
        UserName = userName;
        Roles = roles;
    }

    public int? UserId { get; }

    public string? UserName { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public bool IsInRole(string role) => Roles.Contains(role);
}

/// <summary>
/// Impresora simulada. Cuenta las invocaciones para poder afirmar que un rechazo
/// no imprime: aprobar y no imprimir seria un bug silencioso, pero rechazar e
/// imprimir de todas formas seria mercancia mal etiquetada en piso.
/// </summary>
public sealed class SpyPrintSimulator : IPrintSimulator
{
    /// <summary>Veces que se invoco la impresion.</summary>
    public int Invocations { get; private set; }

    public Task<string> PrintAsync(Label label, Guid correlationId, CancellationToken cancellationToken = default)
    {
        Invocations++;
        return Task.FromResult(label.Zpl);
    }
}
