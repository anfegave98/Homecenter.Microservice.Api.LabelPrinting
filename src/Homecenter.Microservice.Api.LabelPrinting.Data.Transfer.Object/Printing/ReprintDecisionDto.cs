using System.ComponentModel.DataAnnotations;

namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Decision de un Supervisor o Admin sobre una reimpresion que quedo pendiente.
///
/// El autorizador no viaja en el body: se toma del JWT, por la misma razon que el
/// solicitante. Una autorizacion que el cliente pueda declarar no es un control.
/// </summary>
public sealed class ReprintDecisionDto
{
    /// <summary>
    /// Comentario del autorizador. Obligatorio al negar: un rechazo sin explicacion
    /// deja al operario sin saber que corregir y a soporte sin rastro de por que se
    /// nego el duplicado.
    /// </summary>
    [MaxLength(300)]
    public string? Note { get; set; }
}
