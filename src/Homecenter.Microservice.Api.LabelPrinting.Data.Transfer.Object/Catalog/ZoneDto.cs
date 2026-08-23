namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Catalog;

public sealed class ZoneDto
{
    public required int Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }
}
