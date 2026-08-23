using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Regla 4: politica de reimpresion.
///
/// A diferencia de las reglas anteriores, esta no rechaza por el estado de los datos
/// sino por quien pide y con que justificacion. Una reimpresion duplica una etiqueta
/// que ya esta circulando en piso, y dos bultos con la misma ETQ es un problema
/// operativo real: por eso exige rol autorizado y motivo registrado.
///
/// Cuando no existe impresion previa, la regla no aplica y deja pasar.
/// </summary>
public sealed class ReprintPolicyRule : IPrintRule
{
    private static readonly string[] AuthorizedRoles =
    {
        RoleName.Supervisor,
        RoleName.Admin
    };

    /// <summary>
    /// Se evalua de ultima: solo tiene sentido preguntar quien autoriza la reimpresion
    /// despues de confirmar que la etiqueta existe y que sus datos permiten imprimir.
    /// </summary>
    public int Order => 4;

    /// <summary>Evalua la politica de reimpresion sobre el contexto recibido.</summary>
    /// <param name="context">Contexto con la etiqueta resuelta, el usuario y sus roles.</param>
    /// <returns>Resultado aprobatorio o de rechazo con su codigo de contrato.</returns>
    public PrintRuleResult Evaluate(PrintRuleContext context)
    {
        if (!context.HasPreviousPrint)
        {
            return PrintRuleResult.Pass(RuleCodes.ReprintPolicy, "Primera impresion de la ETQ.");
        }

        var isAuthorized = context.UserRoles.Any(role => AuthorizedRoles.Contains(role));

        if (!isAuthorized)
        {
            return PrintRuleResult.Fail(
                RuleCodes.ReprintPolicy,
                RejectionCodes.ReprintNotAuthorized,
                $"La ETQ/LPN '{context.RequestedKey}' ya fue impresa. La reimpresion requiere "
                + $"un usuario con rol {RoleName.Supervisor} o {RoleName.Admin}.");
        }

        if (string.IsNullOrWhiteSpace(context.ReprintReason))
        {
            return PrintRuleResult.Fail(
                RuleCodes.ReprintPolicy,
                RejectionCodes.ReprintReasonRequired,
                $"La ETQ/LPN '{context.RequestedKey}' ya fue impresa. Debe indicar el motivo de la reimpresion.");
        }

        return PrintRuleResult.Pass(
            RuleCodes.ReprintPolicy,
            $"Reimpresion autorizada. Motivo: {context.ReprintReason}");
    }
}
