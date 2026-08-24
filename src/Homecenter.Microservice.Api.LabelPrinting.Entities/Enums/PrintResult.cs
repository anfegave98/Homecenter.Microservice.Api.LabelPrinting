namespace Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

/// <summary>
/// Desenlace de una solicitud de impresion. Todos los desenlaces se auditan.
/// </summary>
public enum PrintResult
{
    /// <summary>La solicitud cumplio todas las reglas y la etiqueta se imprimio.</summary>
    Approved = 1,

    /// <summary>La solicitud incumplio alguna regla y no se imprimio.</summary>
    Rejected = 2,

    /// <summary>
    /// Reimpresion solicitada por un usuario sin rol autorizado.
    ///
    /// No se imprimio, pero tampoco esta cerrada: queda esperando la decision de un
    /// Supervisor o Admin. Es el unico estado no terminal de la auditoria.
    /// </summary>
    PendingApproval = 3
}
