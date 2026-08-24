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

    /// <summary>Crea una instancia con sus dependencias.</summary>
    public PrintRuleEngine(IEnumerable<IPrintRule> rules)
    {
        _rules = rules.OrderBy(rule => rule.Order).ToList();
    }

    /// <summary>Evalua las reglas registradas en orden, cortando en la primera violacion.</summary>
    /// <param name="context">Contexto con todos los insumos ya resueltos.</param>
    /// <returns>Traza de lo evaluado y la regla que fallo, si hubo alguna.</returns>
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
    /// <summary>Reglas evaluadas hasta el corte, en orden.</summary>
    public required IReadOnlyCollection<PrintRuleResult> Trace { get; init; }

    /// <summary>Regla que fallo. Null si todas se cumplieron.</summary>
    public PrintRuleResult? Failure { get; init; }

    /// <summary>True cuando ninguna regla fue violada.</summary>
    public bool IsApproved => Failure is null;

    /// <summary>
    /// True cuando la solicitud no se cierra: quedo esperando que un rol autorizado
    /// la resuelva.
    /// </summary>
    public bool RequiresApproval => Failure?.RequiresApproval == true;

    /// <summary>True cuando alguna regla cerro la solicitud con un rechazo definitivo.</summary>
    public bool IsRejected => Failure is not null && !Failure.RequiresApproval;
}
