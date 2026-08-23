using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Regla 0: la solicitud debe traer los datos minimos para poder decidir.
/// </summary>
public sealed class RequiredDataRule : IPrintRule
{
    /// <summary>Orden de evaluacion dentro del motor de reglas.</summary>
    public int Order => 0;

    /// <summary>Evalua la regla sobre el contexto recibido.</summary>
    /// <param name="context">Contexto con la etiqueta, el documento, la zona y el inventario ya resueltos.</param>
    /// <returns>Resultado aprobatorio o de rechazo con su codigo de contrato.</returns>
    public PrintRuleResult Evaluate(PrintRuleContext context)
    {
        var faltantes = new List<string>();

        if (string.IsNullOrWhiteSpace(context.RequestedKey))
        {
            faltantes.Add("lpn");
        }

        if (string.IsNullOrWhiteSpace(context.UserName))
        {
            faltantes.Add("usuario");
        }

        if (faltantes.Count > 0)
        {
            return PrintRuleResult.Fail(
                RuleCodes.RequiredData,
                RejectionCodes.MissingRequiredData,
                $"Faltan datos obligatorios en la solicitud: {string.Join(", ", faltantes)}.",
                faltantes.Cast<object>().ToArray());
        }

        return PrintRuleResult.Pass(RuleCodes.RequiredData);
    }
}
