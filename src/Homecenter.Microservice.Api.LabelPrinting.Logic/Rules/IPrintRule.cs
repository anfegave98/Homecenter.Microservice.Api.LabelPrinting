namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Regla de negocio evaluada al momento de imprimir.
///
/// Es sincrona y pura a proposito: no consulta base de datos ni servicios externos.
/// Todos sus insumos llegan resueltos en el contexto.
/// </summary>
public interface IPrintRule
{
    /// <summary>Orden de evaluacion. Menor se evalua primero.</summary>
    int Order { get; }

    /// <summary>Evalua la regla sobre el contexto recibido.</summary>
    /// <param name="context">Contexto con todos los insumos ya resueltos.</param>
    /// <returns>Resultado aprobatorio o de rechazo.</returns>
    PrintRuleResult Evaluate(PrintRuleContext context);
}
