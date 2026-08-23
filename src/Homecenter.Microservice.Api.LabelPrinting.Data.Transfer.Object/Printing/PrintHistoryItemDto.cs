namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Fila del historial. Expone los campos que el enunciado exige mostrar:
/// ETQ, LPN, zona, usuario, fecha, resultado y tipo de evento.
/// </summary>
public sealed class PrintHistoryItemDto
{
    /// <summary>Identificador del registro de auditoria.</summary>
    public required int Id { get; init; }

    /// <summary>Llave de rastreo del caso en logs y auditoria.</summary>
    public required Guid CorrelationId { get; init; }

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

    /// <summary>APPROVED o REJECTED.</summary>
    public required string Result { get; init; }

    /// <summary>PRINT o REPRINT.</summary>
    public required string EventType { get; init; }

    /// <summary>Codigo de rechazo, si la solicitud no procedio.</summary>
    public string? RejectionCode { get; init; }

    /// <summary>Motivo legible del rechazo.</summary>
    public string? RejectionMessage { get; init; }

    /// <summary>Justificacion capturada en caso de reimpresion.</summary>
    public string? ReprintReason { get; init; }

    /// <summary>Fecha y hora de procesamiento, en UTC.</summary>
    public required DateTimeOffset ProcessedAt { get; init; }
}
