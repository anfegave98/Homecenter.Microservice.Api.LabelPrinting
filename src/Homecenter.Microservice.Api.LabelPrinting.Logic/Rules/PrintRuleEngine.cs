namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Evalua las reglas en orden y corta en la primera violacion.
///
/// El corte temprano es intencional: si el LPN no existe, validar inventario no tiene
/// sentido y confundiria el motivo del rechazo. Aun asi se devuelve la traza completa
/// de lo evaluado hasta el corte, porque la auditoria debe poder mostrar que se reviso
/// y no solo que fallo.
/// </summary>
public sealed class PrintRuleEngine
{
    private readonly IReadOnlyList<IPrintRule> _rules;

    public PrintRuleEngine(IEnumerable<IPrintRule> rules)
    {
        _rules = rules.OrderBy(rule => rule.Order).ToList();
    }

    public PrintRuleEvaluation Evaluate(PrintRuleContext context)
    {
        var trace = new List<PrintRuleResult>();

        foreach (var rule in _rules)
        {
            var result = rule.Evaluate(context);
            trace.Add(result);

            if (!result.Passed)
            {
                return new PrintRuleEvaluation { Trace = trace, Failure = result };
            }
        }

        return new PrintRuleEvaluation { Trace = trace, Failure = null };
    }
}

/// <summary>
/// Desenlace de la evaluacion completa: la traza de reglas y, si hubo, la que fallo.
/// </summary>
public sealed class PrintRuleEvaluation
{
    public required IReadOnlyCollection<PrintRuleResult> Trace { get; init; }

    public PrintRuleResult? Failure { get; init; }

    public bool IsApproved => Failure is null;
}
