using System.ComponentModel.DataAnnotations;

namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Solicitud de impresion.
///
/// El usuario NO viaja en el body: se toma del JWT. Si el operario pudiera declararlo,
/// la auditoria dejaria de ser un control y pasaria a ser un campo de texto libre.
/// </summary>
public sealed class PrintRequestCreateDto
{
    /// <summary>Identificador de la unidad logistica o de la etiqueta. Se acepta LPN o ETQ.</summary>
    [Required(ErrorMessage = "El LPN es obligatorio.")]
    [MaxLength(50)]
    public string Lpn { get; set; } = string.Empty;

    /// <summary>Zona solicitada. Si se omite, se usa la zona del documento origen.</summary>
    [MaxLength(50)]
    public string? ZoneCode { get; set; }

    /// <summary>Obligatorio cuando la solicitud resulta ser una reimpresion.</summary>
    [MaxLength(300)]
    public string? ReprintReason { get; set; }
}
