namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Resultado de una solicitud de impresion, tanto aprobada como rechazada.
/// El CorrelationId es la llave para rastrear el caso en logs y auditoria.
/// </summary>
public sealed class PrintResultDto
{
    /// <summary>Llave de rastreo del caso en logs, auditoria y archivo ZPL generado.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>APPROVED, REJECTED o PENDING_APPROVAL.</summary>
    public required string Result { get; init; }

    /// <summary>
    /// Identificador de la solicitud en la auditoria.
    ///
    /// Se expone porque una reimpresion pendiente no termina aqui: es la llave con la
    /// que el Supervisor la aprueba o la niega despues.
    /// </summary>
    public int? RequestId { get; init; }

    /// <summary>PRINT o REPRINT.</summary>
    public required string EventType { get; init; }

    /// <summary>Etiqueta resuelta. Null si el LPN no existia.</summary>
    public string? EtqId { get; init; }

    /// <summary>Unidad logistica solicitada.</summary>
    public required string LpnId { get; init; }

    /// <summary>Zona contra la que se valido.</summary>
    public string? ZoneCode { get; init; }

    /// <summary>Usuario que ejecuto la solicitud, tomado del token.</summary>
    public required string UserName { get; init; }

    /// <summary>Documento origen asociado.</summary>
    public string? DocumentNumber { get; init; }

    /// <summary>Fecha y hora de procesamiento, en UTC.</summary>
    public required DateTimeOffset ProcessedAt { get; init; }

    /// <summary>Justificacion registrada cuando el evento es una reimpresion.</summary>
    public string? ReprintReason { get; init; }

    /// <summary>Contenido ZPL listo para enviar a impresora. Solo en resultado aprobado.</summary>
    public string? Zpl { get; init; }

    /// <summary>Productos de la ETQ con su situacion de inventario al momento de decidir.</summary>
    public IReadOnlyCollection<ProductAvailabilityDto>? Products { get; init; }

    /// <summary>Usuario que aprobo o nego la reimpresion. Null mientras siga pendiente.</summary>
    public string? ApprovedBy { get; init; }

    /// <summary>Fecha y hora de la decision del autorizador, en UTC.</summary>
    public DateTimeOffset? DecidedAt { get; init; }

    /// <summary>Comentario que dejo el autorizador al resolver la solicitud.</summary>
    public string? ApprovalNote { get; init; }

    /// <summary>Bloque compatible con el contrato responseEtq.json. Solo en resultado aprobado.</summary>
    public LegacyEtqResponseDto? Legacy { get; init; }
}
