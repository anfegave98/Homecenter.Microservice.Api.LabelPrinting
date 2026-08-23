using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Solicitud de impresion procesada. Es el registro de auditoria del submodulo.
///
/// Se persiste SIEMPRE, apruebe o rechace: una auditoria que solo guarda los exitos
/// no sirve para soporte, porque las fallas son justamente lo que hay que investigar.
/// Tambien es la fuente que resuelve la deteccion de reimpresion (Regla 4).
/// </summary>
public class PrintRequest : EntityBase
{
    /// <summary>Llave de rastreo del caso en logs, auditoria y archivo ZPL generado.</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Etiqueta resuelta. Null cuando el LPN solicitado no existia.</summary>
    public string? EtqId { get; set; }

    /// <summary>Unidad logistica solicitada, tal como llego en la peticion.</summary>
    public string LpnId { get; set; } = string.Empty;

    /// <summary>Zona contra la que se valido. Null si no pudo resolverse.</summary>
    public int? IdZone { get; set; }

    /// <summary>Usuario que ejecuto la solicitud, tomado del token.</summary>
    public int IdUser { get; set; }

    /// <summary>Documento origen asociado a la etiqueta.</summary>
    public string? DocumentNumber { get; set; }

    /// <summary>Desenlace de la solicitud.</summary>
    public PrintResult Result { get; set; }

    /// <summary>Indica si fue una primera impresion o una reimpresion.</summary>
    public PrintEventType EventType { get; set; }

    /// <summary>Codigo de rechazo del contrato publico. Null si la solicitud procedio.</summary>
    public string? RejectionCode { get; set; }

    /// <summary>Motivo legible del rechazo.</summary>
    public string? RejectionMessage { get; set; }

    /// <summary>Justificacion capturada cuando el evento es una reimpresion.</summary>
    public string? ReprintReason { get; set; }

    /// <summary>Fecha y hora de procesamiento, en UTC.</summary>
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Zona asociada.</summary>
    public Zone? Zone { get; set; }

    /// <summary>Usuario solicitante.</summary>
    public User User { get; set; } = null!;

    /// <summary>Traza de las reglas evaluadas durante el procesamiento.</summary>
    public ICollection<PrintAuditLog> AuditLogs { get; set; } = new List<PrintAuditLog>();
}
