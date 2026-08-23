namespace Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

/// <summary>
/// Estado del documento origen (nota pedido / orden).
/// Solo ANULADA y DEVUELTA bloquean la impresion (Regla 2 del enunciado);
/// CREADA y LIBERADA la permiten.
/// </summary>
public enum DocumentStatus
{
    Creada = 1,
    Liberada = 2,
    Anulada = 3,
    Devuelta = 4
}
