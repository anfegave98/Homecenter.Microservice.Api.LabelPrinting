namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Vista de consulta de una ETQ/LPN: documento, productos y disponibilidad por zona.
///
/// Es solo lectura y NO imprime ni audita. La validacion vinculante ocurre siempre
/// al imprimir, tal como exige el enunciado: entre esta consulta y la impresion el
/// inventario pudo cambiar.
/// </summary>
public sealed class LabelDetailDto
{
    public required string EtqId { get; init; }

    public required string LpnId { get; init; }

    public required bool IsPreGenerated { get; init; }

    public required string TemplateCode { get; init; }

    public required DocumentSummaryDto Document { get; init; }

    public required string ZoneCode { get; init; }

    public required IReadOnlyCollection<ProductAvailabilityDto> Products { get; init; }

    /// <summary>Indica si la ETQ ya fue impresa: la proxima solicitud seria reimpresion.</summary>
    public required bool HasPreviousPrint { get; init; }

    /// <summary>Pronostico informativo del resultado, sujeto a revalidacion al imprimir.</summary>
    public required bool CanPrint { get; init; }

    public string? BlockingReason { get; init; }
}
