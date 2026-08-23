using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Regla 0: la solicitud debe traer los datos minimos para poder decidir.
/// </summary>
public sealed class RequiredDataRule : IPrintRule
{
    public int Order => 0;

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
