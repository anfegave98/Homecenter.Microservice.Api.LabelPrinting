namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Veredicto de una regla. Incluye el codigo de la regla evaluada para poder
/// registrarlo en la auditoria aunque el resultado sea satisfactorio.
/// </summary>
public sealed class PrintRuleResult
{
    /// <summary>Identificador de la regla evaluada.</summary>
    public required string RuleCode { get; init; }

    /// <summary>Indica si la regla se cumplio.</summary>
    public required bool Passed { get; init; }

    /// <summary>Codigo de rechazo del contrato publico. Null si la regla paso.</summary>
    public string? RejectionCode { get; init; }

    /// <summary>Detalle legible de lo evaluado o del incumplimiento.</summary>
    public string? Message { get; init; }

    /// <summary>Detalle granular del incumplimiento, expuesto en error.details.</summary>
    public IReadOnlyCollection<object>? Details { get; init; }

    /// <inheritdoc />
    public static PrintRuleResult Pass(string ruleCode, string? detail = null) =>
        new() { RuleCode = ruleCode, Passed = true, Message = detail };

    /// <inheritdoc />
    public static PrintRuleResult Fail(
        string ruleCode,
        string rejectionCode,
        string message,
        IReadOnlyCollection<object>? details = null) =>
        new()
        {
            RuleCode = ruleCode,
            Passed = false,
            RejectionCode = rejectionCode,
            Message = message,
            Details = details
        };
}
