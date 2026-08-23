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
    /// <summary>Identificador de la etiqueta.</summary>
    public required string EtqId { get; init; }

    /// <summary>Identificador de la unidad logistica.</summary>
    public required string LpnId { get; init; }

    /// <summary>Confirma que la etiqueta ya venia generada por el proceso de olas.</summary>
    public required bool IsPreGenerated { get; init; }

    /// <summary>Codigo de la plantilla de impresion.</summary>
    public required string TemplateCode { get; init; }

    /// <summary>Documento origen de la etiqueta.</summary>
    public required DocumentSummaryDto Document { get; init; }

    /// <summary>Zona contra la que se consulto la disponibilidad.</summary>
    public required string ZoneCode { get; init; }

    /// <summary>Productos de la ETQ con su situacion de inventario.</summary>
    public required IReadOnlyCollection<ProductAvailabilityDto> Products { get; init; }

    /// <summary>Indica si la ETQ ya fue impresa: la proxima solicitud seria reimpresion.</summary>
    public required bool HasPreviousPrint { get; init; }

    /// <summary>Pronostico informativo del resultado, sujeto a revalidacion al imprimir.</summary>
    public required bool CanPrint { get; init; }

    /// <summary>Motivo por el que hoy no seria posible imprimir. Null si no hay impedimento.</summary>
    public string? BlockingReason { get; init; }
}
