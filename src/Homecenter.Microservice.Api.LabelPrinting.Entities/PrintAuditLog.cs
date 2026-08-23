namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Traza de la evaluacion de cada regla dentro de una solicitud.
/// Permite responder "por que se rechazo" sin reconstruir el caso a mano,
/// que es exactamente lo que se necesita durante un incidente productivo.
/// </summary>
public class PrintAuditLog : EntityBase
{
    public int IdPrintRequest { get; set; }

    public string RuleCode { get; set; } = string.Empty;

    public bool Passed { get; set; }

    public string? Detail { get; set; }

    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;

    public PrintRequest PrintRequest { get; set; } = null!;
}
