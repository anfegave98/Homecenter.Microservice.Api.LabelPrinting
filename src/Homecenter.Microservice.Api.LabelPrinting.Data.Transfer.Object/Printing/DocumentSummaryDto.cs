namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>Resumen del documento origen asociado a la etiqueta.</summary>
public sealed class DocumentSummaryDto
{
    /// <summary>Tipo de documento, por ejemplo NOTA_PEDIDO.</summary>
    public required string DocumentType { get; init; }

    /// <summary>Numero del documento.</summary>
    public required string DocumentNumber { get; init; }

    /// <summary>Estado del documento: CREADA, LIBERADA, ANULADA o DEVUELTA.</summary>
    public required string Status { get; init; }

    /// <summary>Identificador transaccional de la solicitud origen.</summary>
    public required string RequestId { get; init; }

    /// <summary>Usuario que genero el documento en el sistema origen.</summary>
    public required string RequestedBy { get; init; }
}
