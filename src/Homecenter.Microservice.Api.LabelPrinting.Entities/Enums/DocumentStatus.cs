namespace Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

/// <summary>
/// Estado del documento origen (nota pedido / orden).
/// Solo ANULADA y DEVUELTA bloquean la impresion (Regla 2 del enunciado);
/// CREADA y LIBERADA la permiten.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Documento registrado pero aun no liberado a operacion. Permite imprimir.</summary>
    Creada = 1,

    /// <summary>Documento liberado para su ejecucion en piso. Permite imprimir.</summary>
    Liberada = 2,

    /// <summary>Documento anulado. Bloquea la impresion.</summary>
    Anulada = 3,

    /// <summary>Documento devuelto por el cliente u operacion. Bloquea la impresion.</summary>
    Devuelta = 4
}
