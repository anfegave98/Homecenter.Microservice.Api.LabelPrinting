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

    /// <summary>
    /// True cuando la regla no dejo pasar la solicitud pero tampoco la cierra: queda
    /// esperando la decision de un tercero.
    ///
    /// Se modela sobre Passed=false y no como un desenlace independiente porque el
    /// motor solo necesita saber si debe cortar; que hacer con el corte lo decide el
    /// caso de uso.
    /// </summary>
    public bool RequiresApproval { get; init; }

    /// <inheritdoc />
    public static PrintRuleResult Pass(string ruleCode, string? detail = null) =>
        new() { RuleCode = ruleCode, Passed = true, Message = detail };

    /// <summary>
    /// La regla no deja imprimir ahora, pero deriva la solicitud a autorizacion en
    /// lugar de cerrarla.
    /// </summary>
    /// <param name="ruleCode">Regla que derivo la solicitud.</param>
    /// <param name="rejectionCode">Codigo del contrato publico que explica la derivacion.</param>
    /// <param name="message">Detalle legible para el usuario.</param>
    /// <returns>Veredicto en espera de autorizacion.</returns>
    public static PrintRuleResult Defer(string ruleCode, string rejectionCode, string message) =>
        new()
        {
            RuleCode = ruleCode,
            Passed = false,
            RequiresApproval = true,
            RejectionCode = rejectionCode,
            Message = message
        };

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
