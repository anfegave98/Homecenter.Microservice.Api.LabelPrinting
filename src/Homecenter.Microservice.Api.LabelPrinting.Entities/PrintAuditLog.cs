namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Traza de la evaluacion de cada regla dentro de una solicitud.
/// Permite responder "por que se rechazo" sin reconstruir el caso a mano,
/// que es exactamente lo que se necesita durante un incidente productivo.
/// </summary>
public class PrintAuditLog : EntityBase
{
    /// <summary>Solicitud de impresion a la que pertenece la traza.</summary>
    public int IdPrintRequest { get; set; }

    /// <summary>Identificador de la regla evaluada, por ejemplo R3_ZONE_AVAILABILITY.</summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>Resultado de la evaluacion de la regla.</summary>
    public bool Passed { get; set; }

    /// <summary>Detalle legible de lo evaluado o del incumplimiento.</summary>
    public string? Detail { get; set; }

    /// <summary>Fecha y hora de la evaluacion, en UTC.</summary>
    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Solicitud asociada.</summary>
    public PrintRequest PrintRequest { get; set; } = null!;
}
