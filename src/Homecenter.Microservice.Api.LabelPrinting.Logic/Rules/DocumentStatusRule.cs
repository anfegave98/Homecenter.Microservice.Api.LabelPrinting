using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Regla 2: no se imprime sobre documentos ANULADA ni DEVUELTA.
///
/// Se declara la lista de estados bloqueados en vez de la de permitidos: si el dia
/// de manana aparece un estado nuevo, el comportamiento por defecto es permitir la
/// impresion, que es lo que el enunciado define.
/// </summary>
public sealed class DocumentStatusRule : IPrintRule
{
    private static readonly DocumentStatus[] BlockedStatuses =
    {
        DocumentStatus.Anulada,
        DocumentStatus.Devuelta
    };

    /// <summary>Orden de evaluacion dentro del motor de reglas.</summary>
    public int Order => 2;

    /// <summary>Evalua la regla sobre el contexto recibido.</summary>
    /// <param name="context">Contexto con la etiqueta, el documento, la zona y el inventario ya resueltos.</param>
    /// <returns>Resultado aprobatorio o de rechazo con su codigo de contrato.</returns>
    public PrintRuleResult Evaluate(PrintRuleContext context)
    {
        if (context.Document is null)
        {
            return PrintRuleResult.Pass(RuleCodes.DocumentStatus, "Sin documento que evaluar.");
        }

        var status = context.Document.Status;

        if (BlockedStatuses.Contains(status))
        {
            return PrintRuleResult.Fail(
                RuleCodes.DocumentStatus,
                RejectionCodes.InvalidDocumentStatus,
                $"El documento {context.Document.DocumentNumber} se encuentra en estado "
                + $"{status.ToString().ToUpperInvariant()} y no admite impresion.");
        }

        return PrintRuleResult.Pass(RuleCodes.DocumentStatus, $"Documento en estado {status}.");
    }
}
