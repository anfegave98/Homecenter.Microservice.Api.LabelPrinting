using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Regla 3: todos los productos de la ETQ deben tener disponibilidad suficiente
/// Y estar abastecidos en la zona solicitada.
///
/// Son dos condiciones independientes. Un producto puede tener stock registrado y aun
/// asi no estar abastecido para operar en esa zona, y el enunciado exige ambas.
///
/// Se evaluan TODOS los productos antes de responder, en lugar de cortar en el primer
/// incumplimiento: al operario le sirve saber de una vez todo lo que le falta.
/// </summary>
public sealed class ZoneAvailabilityRule : IPrintRule
{
    private const string InsufficientReason = "Disponibilidad insuficiente en la zona.";
    private const string NotStockedReason = "El producto no esta abastecido en la zona.";
    private const string NoRecordReason = "El producto no tiene registro de inventario en la zona.";

    /// <summary>Orden de evaluacion dentro del motor de reglas.</summary>
    public int Order => 3;

    /// <summary>Evalua la regla sobre el contexto recibido.</summary>
    /// <param name="context">Contexto con la etiqueta, el documento, la zona y el inventario ya resueltos.</param>
    /// <returns>Resultado aprobatorio o de rechazo con su codigo de contrato.</returns>
    public PrintRuleResult Evaluate(PrintRuleContext context)
    {
        if (context.Label is null || context.Zone is null)
        {
            return PrintRuleResult.Pass(RuleCodes.ZoneAvailability, "Sin etiqueta o zona que evaluar.");
        }

        if (context.Products.Count == 0)
        {
            return PrintRuleResult.Fail(
                RuleCodes.ZoneAvailability,
                RejectionCodes.InsufficientInventory,
                "La ETQ no tiene productos asociados: no hay nada que validar ni imprimir.");
        }

        var shortages = new List<InventoryShortageDto>();
        var hasStockShortage = false;
        var hasNotStocked = false;

        foreach (var line in context.Products)
        {
            context.Availability.TryGetValue(line.IdProduct, out var availability);

            var availableQty = availability?.AvailableQty ?? 0m;
            var isStocked = availability?.IsStocked ?? false;

            // El limite es inclusivo: solicitar exactamente lo disponible es valido.
            var hasEnough = availableQty >= line.RequestedQty;

            if (hasEnough && isStocked)
            {
                continue;
            }

            string reason;
            if (availability is null)
            {
                reason = NoRecordReason;
                hasStockShortage = true;
            }
            else if (!isStocked)
            {
                reason = NotStockedReason;
                hasNotStocked = true;
            }
            else
            {
                reason = InsufficientReason;
                hasStockShortage = true;
            }

            shortages.Add(new InventoryShortageDto
            {
                ProductCode = line.Product?.ProductCode ?? line.IdProduct.ToString(),
                ProductDescription = line.Product?.ProductDescription ?? string.Empty,
                RequestedQty = line.RequestedQty,
                AvailableQty = availableQty,
                IsStocked = isStocked,
                Reason = reason
            });
        }

        if (shortages.Count == 0)
        {
            return PrintRuleResult.Pass(
                RuleCodes.ZoneAvailability,
                $"{context.Products.Count} producto(s) disponibles y abastecidos en {context.Zone.Code}.");
        }

        // Si el unico problema es abastecimiento, el codigo lo refleja; si hay faltante
        // de cantidad (solo o combinado), prima el codigo de disponibilidad.
        var rejectionCode = hasStockShortage || !hasNotStocked
            ? RejectionCodes.InsufficientInventory
            : RejectionCodes.NotStocked;

        var message = $"La zona {context.Zone.Code} no cumple las condiciones de inventario "
                    + $"para {shortages.Count} producto(s) de la ETQ.";

        return PrintRuleResult.Fail(
            RuleCodes.ZoneAvailability,
            rejectionCode,
            message,
            shortages.Cast<object>().ToArray());
    }
}
