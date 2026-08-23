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
    public Guid CorrelationId { get; set; }

    public string? EtqId { get; set; }

    public string LpnId { get; set; } = string.Empty;

    public int? IdZone { get; set; }

    public int IdUser { get; set; }

    public string? DocumentNumber { get; set; }

    public PrintResult Result { get; set; }

    public PrintEventType EventType { get; set; }

    public string? RejectionCode { get; set; }

    public string? RejectionMessage { get; set; }

    public string? ReprintReason { get; set; }

    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;

    public Zone? Zone { get; set; }

    public User User { get; set; } = null!;

    public ICollection<PrintAuditLog> AuditLogs { get; set; } = new List<PrintAuditLog>();
}
