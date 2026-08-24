using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Services;

/// <summary>
/// Arma el contexto que consumen las reglas: resuelve etiqueta, zona, productos,
/// disponibilidad e historial previo.
///
/// Vive aparte del caso de uso porque hay dos momentos que necesitan exactamente el
/// mismo contexto: cuando el operario envia la solicitud y cuando el supervisor
/// resuelve una reimpresion pendiente. Ese segundo momento ocurre despues, y el
/// inventario pudo cambiar entre uno y otro: si la aprobacion reutilizara el veredicto
/// viejo en lugar de recalcularlo, autorizaria imprimir sobre stock que ya no existe.
/// </summary>
public sealed class PrintContextBuilder
{
    private readonly ILabelRepository _labelRepository;
    private readonly IZoneRepository _zoneRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPrintRequestRepository _printRequestRepository;
    private readonly ICurrentUserAccessor _currentUser;

    /// <summary>Crea el constructor de contexto con sus dependencias.</summary>
    /// <param name="labelRepository">Acceso a etiquetas pre-generadas.</param>
    /// <param name="zoneRepository">Acceso al catalogo de zonas.</param>
    /// <param name="inventoryRepository">Acceso a la disponibilidad por zona.</param>
    /// <param name="printRequestRepository">Acceso a la auditoria de impresiones.</param>
    /// <param name="currentUser">Identidad tomada del token.</param>
    public PrintContextBuilder(
        ILabelRepository labelRepository,
        IZoneRepository zoneRepository,
        IInventoryRepository inventoryRepository,
        IPrintRequestRepository printRequestRepository,
        ICurrentUserAccessor currentUser)
    {
        _labelRepository = labelRepository;
        _zoneRepository = zoneRepository;
        _inventoryRepository = inventoryRepository;
        _printRequestRepository = printRequestRepository;
        _currentUser = currentUser;
    }

    /// <summary>Resuelve todos los insumos que las reglas necesitan para decidir.</summary>
    /// <param name="requestedKey">LPN o ETQ solicitado.</param>
    /// <param name="requestedZoneCode">Zona indicada. Si es nula se usa la del documento origen.</param>
    /// <param name="reprintReason">Motivo de reimpresion informado, si aplica.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Contexto listo para el motor de reglas.</returns>
    public async Task<PrintRuleContext> BuildAsync(
        string requestedKey,
        string? requestedZoneCode,
        string? reprintReason,
        CancellationToken cancellationToken = default)
    {
        var label = await _labelRepository.GetByLpnOrEtqAsync(requestedKey, cancellationToken);
        var document = label?.Document;

        // Si el operario no indica zona, se usa la del documento origen.
        var zone = await ResolveZoneAsync(requestedZoneCode, document, cancellationToken);

        var products = document?.DocumentProducts.Where(x => x.State).ToArray() ?? Array.Empty<DocumentProduct>();

        var availability = new Dictionary<int, InventoryAvailability>();
        if (zone is not null && products.Length > 0)
        {
            var rows = await _inventoryRepository.GetByProductsAndZoneAsync(
                products.Select(x => x.IdProduct).Distinct().ToArray(),
                zone.Id,
                cancellationToken);

            availability = rows.ToDictionary(x => x.IdProduct);
        }

        var hasPreviousPrint = label is not null
            && await _printRequestRepository.HasApprovedPrintAsync(label.LpnId, cancellationToken);

        return new PrintRuleContext
        {
            RequestedKey = requestedKey,
            RequestedZoneCode = requestedZoneCode ?? document?.Zone?.Code,
            ReprintReason = reprintReason,
            UserName = _currentUser.UserName ?? string.Empty,
            UserRoles = _currentUser.Roles,
            Label = label,
            Document = document,
            Zone = zone,
            Products = products,
            Availability = availability,
            HasPreviousPrint = hasPreviousPrint
        };
    }

    private async Task<Zone?> ResolveZoneAsync(
        string? requestedZoneCode,
        Document? document,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedZoneCode))
        {
            return await _zoneRepository.GetByCodeAsync(requestedZoneCode.Trim(), cancellationToken);
        }

        return document?.Zone;
    }
}
