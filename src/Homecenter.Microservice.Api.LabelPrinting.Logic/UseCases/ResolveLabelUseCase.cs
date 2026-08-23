using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;

/// <summary>
/// Consulta de una ETQ/LPN: documento, productos y disponibilidad en la zona.
///
/// Es estrictamente de lectura: no imprime, no audita y su veredicto CanPrint es
/// informativo. La validacion vinculante ocurre al imprimir, porque entre la consulta
/// y la impresion el inventario o el estado del documento pudieron cambiar.
/// </summary>
public sealed class ResolveLabelUseCase : IResolveLabelUseCase
{
    private static readonly DocumentStatus[] BlockedStatuses =
    {
        DocumentStatus.Anulada,
        DocumentStatus.Devuelta
    };

    private readonly ILabelRepository _labelRepository;
    private readonly IZoneRepository _zoneRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPrintRequestRepository _printRequestRepository;

    /// <summary>Crea una instancia con sus dependencias.</summary>
    public ResolveLabelUseCase(
        ILabelRepository labelRepository,
        IZoneRepository zoneRepository,
        IInventoryRepository inventoryRepository,
        IPrintRequestRepository printRequestRepository)
    {
        _labelRepository = labelRepository;
        _zoneRepository = zoneRepository;
        _inventoryRepository = inventoryRepository;
        _printRequestRepository = printRequestRepository;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<LabelDetailDto>> ExecuteAsync(
        string lpn,
        string? zoneCode,
        CancellationToken cancellationToken = default)
    {
        var label = await _labelRepository.GetByLpnOrEtqAsync(lpn, cancellationToken);

        if (label?.Document is null)
        {
            return ApiResponse<LabelDetailDto>.Fail(
                RejectionCodes.LpnNotFound,
                $"No existe una ETQ/LPN registrada con el identificador '{lpn}'.");
        }

        var document = label.Document;

        var zone = string.IsNullOrWhiteSpace(zoneCode)
            ? document.Zone
            : await _zoneRepository.GetByCodeAsync(zoneCode.Trim(), cancellationToken);

        if (zone is null)
        {
            return ApiResponse<LabelDetailDto>.Fail(
                RejectionCodes.ZoneNotFound,
                $"La zona '{zoneCode}' no existe o no esta activa.");
        }

        var lines = document.DocumentProducts.Where(x => x.State).ToArray();

        var availability = new Dictionary<int, InventoryAvailability>();
        if (lines.Length > 0)
        {
            var rows = await _inventoryRepository.GetByProductsAndZoneAsync(
                lines.Select(x => x.IdProduct).Distinct().ToArray(),
                zone.Id,
                cancellationToken);

            availability = rows.ToDictionary(x => x.IdProduct);
        }

        var products = lines.Select(line =>
        {
            availability.TryGetValue(line.IdProduct, out var row);
            var availableQty = row?.AvailableQty ?? 0m;
            var isStocked = row?.IsStocked ?? false;

            return new ProductAvailabilityDto
            {
                ProductCode = line.Product?.ProductCode ?? string.Empty,
                ProductDescription = line.Product?.ProductDescription ?? string.Empty,
                RequestedQty = line.RequestedQty,
                Uom = line.Uom,
                AvailableQty = availableQty,
                IsStocked = isStocked,
                IsEligible = availableQty >= line.RequestedQty && isStocked
            };
        }).ToArray();

        var hasPreviousPrint = await _printRequestRepository.HasApprovedPrintAsync(label.LpnId, cancellationToken);

        var (canPrint, blockingReason) = Forecast(document, products);

        var payload = new LabelDetailDto
        {
            EtqId = label.EtqId,
            LpnId = label.LpnId,
            IsPreGenerated = label.IsPreGenerated,
            TemplateCode = label.TemplateCode,
            Document = new DocumentSummaryDto
            {
                DocumentType = document.DocumentType,
                DocumentNumber = document.DocumentNumber,
                Status = document.Status.ToString().ToUpperInvariant(),
                RequestId = document.RequestId,
                RequestedBy = document.RequestedBy
            },
            ZoneCode = zone.Code,
            Products = products,
            HasPreviousPrint = hasPreviousPrint,
            CanPrint = canPrint,
            BlockingReason = blockingReason
        };

        return ApiResponse<LabelDetailDto>.Ok(payload);
    }

    private static (bool CanPrint, string? Reason) Forecast(
        Document document,
        IReadOnlyCollection<ProductAvailabilityDto> products)
    {
        if (BlockedStatuses.Contains(document.Status))
        {
            return (false, $"El documento se encuentra en estado {document.Status.ToString().ToUpperInvariant()}.");
        }

        if (products.Count == 0)
        {
            return (false, "La ETQ no tiene productos asociados.");
        }

        var blocked = products.Where(x => !x.IsEligible).ToArray();
        if (blocked.Length > 0)
        {
            return (false, $"{blocked.Length} producto(s) sin disponibilidad o sin abastecimiento en la zona.");
        }

        return (true, null);
    }
}
