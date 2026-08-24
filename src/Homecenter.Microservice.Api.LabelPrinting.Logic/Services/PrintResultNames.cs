using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Services;

/// <summary>
/// Traduce los enums de dominio a los literales del contrato publico.
///
/// Existe para que el mapeo viva en un solo lugar: el resultado se expone en el
/// endpoint de impresion, en el de autorizacion y en el historial, y tres copias del
/// mismo switch terminan divergiendo.
/// </summary>
public static class PrintResultNames
{
    /// <summary>Literal publico del desenlace de una solicitud.</summary>
    /// <param name="result">Desenlace de dominio.</param>
    /// <returns>APPROVED, REJECTED o PENDING_APPROVAL.</returns>
    public static string Of(PrintResult result) => result switch
    {
        PrintResult.Approved => "APPROVED",
        PrintResult.PendingApproval => "PENDING_APPROVAL",
        _ => "REJECTED"
    };

    /// <summary>Literal publico del tipo de evento.</summary>
    /// <param name="eventType">Tipo de evento de dominio.</param>
    /// <returns>PRINT o REPRINT.</returns>
    public static string Of(PrintEventType eventType) =>
        eventType == PrintEventType.Reprint ? "REPRINT" : "PRINT";
}
