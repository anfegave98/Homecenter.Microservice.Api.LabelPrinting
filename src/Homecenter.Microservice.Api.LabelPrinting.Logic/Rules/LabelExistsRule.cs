using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Regla 1: la ETQ/LPN debe existir. Tambien valida que la zona solicitada exista:
/// sin zona resuelta no hay contra que validar disponibilidad.
/// </summary>
public sealed class LabelExistsRule : IPrintRule
{
    public int Order => 1;

    public PrintRuleResult Evaluate(PrintRuleContext context)
    {
        if (context.Label is null || context.Document is null)
        {
            return PrintRuleResult.Fail(
                RuleCodes.LabelExists,
                RejectionCodes.LpnNotFound,
                $"No existe una ETQ/LPN registrada con el identificador '{context.RequestedKey}'.");
        }

        if (context.Zone is null)
        {
            return PrintRuleResult.Fail(
                RuleCodes.LabelExists,
                RejectionCodes.ZoneNotFound,
                $"La zona '{context.RequestedZoneCode}' no existe o no esta activa.");
        }

        return PrintRuleResult.Pass(RuleCodes.LabelExists, $"ETQ {context.Label.EtqId} resuelta.");
    }
}
