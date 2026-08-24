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

    /// <summary>
    /// Supervisor o Admin que resolvio una reimpresion que quedo pendiente.
    ///
    /// Se guarda aparte de IdUser a proposito: quien pide la reimpresion y quien la
    /// autoriza son dos personas distintas, y una auditoria que las confunda no sirve
    /// para responder quien aprobo el duplicado.
    /// </summary>
    public int? IdApprover { get; set; }

    /// <summary>Fecha y hora en que se resolvio la solicitud pendiente, en UTC.</summary>
    public DateTimeOffset? DecidedAt { get; set; }

    /// <summary>Justificacion que dejo quien aprobo o nego la reimpresion.</summary>
    public string? ApprovalNote { get; set; }

    /// <summary>
    /// Momento en que la etiqueta se descargo, en UTC. Null mientras nadie la baje.
    ///
    /// Una solicitud aprobada entrega derecho a UNA descarga: la etiqueta fisica sale
    /// una sola vez. Volver a necesitarla es una reimpresion, con su motivo y su
    /// autorizacion, no un segundo clic al mismo boton.
    /// </summary>
    public DateTimeOffset? DownloadedAt { get; set; }

    /// <summary>Usuario que descargo la etiqueta.</summary>
    public int? IdDownloadedBy { get; set; }

    /// <summary>Fecha y hora de procesamiento, en UTC.</summary>
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Zona asociada.</summary>
    public Zone? Zone { get; set; }

    /// <summary>Usuario solicitante.</summary>
    public User User { get; set; } = null!;

    /// <summary>Usuario que resolvio la solicitud pendiente.</summary>
    public User? Approver { get; set; }

    /// <summary>Usuario que descargo la etiqueta.</summary>
    public User? DownloadedBy { get; set; }

    /// <summary>Traza de las reglas evaluadas durante el procesamiento.</summary>
    public ICollection<PrintAuditLog> AuditLogs { get; set; } = new List<PrintAuditLog>();
}
