namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

public class Product : EntityBase
{
    public string ProductCode { get; set; } = string.Empty;

    public string ProductDescription { get; set; } = string.Empty;
}
