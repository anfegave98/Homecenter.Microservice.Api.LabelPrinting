namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Resultado de una solicitud de impresion, tanto aprobada como rechazada.
/// El CorrelationId es la llave para rastrear el caso en logs y auditoria.
/// </summary>
public sealed class PrintResultDto
{
    public required Guid CorrelationId { get; init; }

    /// <summary>APPROVED o REJECTED.</summary>
    public required string Result { get; init; }

    /// <summary>PRINT o REPRINT.</summary>
    public required string EventType { get; init; }

    public string? EtqId { get; init; }

    public required string LpnId { get; init; }

    public string? ZoneCode { get; init; }

    public required string UserName { get; init; }

    public string? DocumentNumber { get; init; }

    public required DateTimeOffset ProcessedAt { get; init; }

    public string? ReprintReason { get; init; }

    /// <summary>Contenido ZPL listo para enviar a impresora. Solo en resultado aprobado.</summary>
    public string? Zpl { get; init; }

    public IReadOnlyCollection<ProductAvailabilityDto>? Products { get; init; }

    public LegacyEtqResponseDto? Legacy { get; init; }
}
