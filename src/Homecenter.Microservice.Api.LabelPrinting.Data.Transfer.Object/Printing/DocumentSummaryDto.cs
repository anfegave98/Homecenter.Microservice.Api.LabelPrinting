namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

public sealed class DocumentSummaryDto
{
    public required string DocumentType { get; init; }

    public required string DocumentNumber { get; init; }

    public required string Status { get; init; }

    public required string RequestId { get; init; }

    public required string RequestedBy { get; init; }
}
