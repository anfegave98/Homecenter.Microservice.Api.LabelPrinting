using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Services;

/// <summary>
/// Arma la respuesta publica de una solicitud a partir del registro de auditoria y del
/// contexto evaluado.
///
/// Se toma la fila persistida como fuente y no las variables locales del caso de uso:
/// lo que se le responde al cliente debe ser exactamente lo que quedo auditado, sin
/// margen para que ambos digan cosas distintas.
/// </summary>
public static class PrintResultMapper
{
    /// <summary>Construye el resultado publico de una solicitud procesada.</summary>
    /// <param name="request">Registro de auditoria ya persistido.</param>
    /// <param name="context">Contexto evaluado por las reglas.</param>
    /// <param name="evaluation">Veredicto del motor de reglas.</param>
    /// <param name="zpl">Contenido ZPL generado, si la solicitud procedio.</param>
    /// <param name="requesterUserName">
    /// Usuario que origino la solicitud. Se recibe aparte porque al resolver una
    /// reimpresion pendiente quien ejecuta es el autorizador, no el solicitante.
    /// </param>
    /// <param name="approverUserName">Autorizador que resolvio la solicitud, si hubo.</param>
    /// <returns>Resultado listo para el contrato publico.</returns>
    public static PrintResultDto Build(
        PrintRequest request,
        PrintRuleContext context,
        PrintRuleEvaluation evaluation,
        string? zpl,
        string? requesterUserName = null,
        string? approverUserName = null)
    {
        var products = context.Products
            .Select(line =>
            {
                context.Availability.TryGetValue(line.IdProduct, out var availability);
                var availableQty = availability?.AvailableQty ?? 0m;
                var isStocked = availability?.IsStocked ?? false;

                return new ProductAvailabilityDto
                {
                    ProductCode = line.Product?.ProductCode ?? string.Empty,
                    ProductDescription = line.Product?.ProductDescription ?? string.Empty,
                    RequestedQty = line.RequestedQty,
                    Uom = line.Uom,
                    AvailableQty = availableQty,
                    IsStocked = isStocked,
                    IsEligible = availableQty >= line.RequestedQty && isStocked
                };
            })
            .ToArray();

        return new PrintResultDto
        {
            CorrelationId = request.CorrelationId,
            RequestId = request.Id,
            Result = PrintResultNames.Of(request.Result),
            EventType = PrintResultNames.Of(request.EventType),
            EtqId = context.Label?.EtqId,
            LpnId = context.RequestedKey,
            ZoneCode = context.Zone?.Code,
            UserName = requesterUserName ?? context.UserName,
            DocumentNumber = context.Document?.DocumentNumber,
            ProcessedAt = request.ProcessedAt,
            ReprintReason = request.ReprintReason,
            ApprovedBy = approverUserName,
            DecidedAt = request.DecidedAt,
            ApprovalNote = request.ApprovalNote,
            Zpl = zpl,
            Products = products.Length > 0 ? products : null,
            Legacy = evaluation.IsApproved ? BuildLegacy(context, zpl) : null
        };
    }

    /// <summary>
    /// Construye el bloque compatible con responseEtq.json.
    /// El anexo expone un unico SKU escalar, asi que se toma el primer producto y se
    /// declara si habia mas: asi el consumidor legacy no rompe, pero tampoco se le
    /// oculta que la ETQ arrastra mas productos de los que ese contrato puede mostrar.
    /// </summary>
    private static LegacyEtqResponseDto? BuildLegacy(PrintRuleContext context, string? zpl)
    {
        if (context.Label is null || context.Document is null)
        {
            return null;
        }

        var first = context.Products.FirstOrDefault();

        return new LegacyEtqResponseDto
        {
            IdEtiqueta = context.Label.EtqId,
            PurchaseOrder = context.Document.DocumentNumber,
            TcOrderId = context.Document.RequestId,
            Sku = first?.Product?.ProductCode ?? string.Empty,
            Unidades = first?.RequestedQty ?? 0m,
            Zpl = zpl ?? context.Label.Zpl,
            HasMultipleProducts = context.Products.Count > 1
        };
    }
}
