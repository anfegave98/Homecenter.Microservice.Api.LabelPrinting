namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

public class Role : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
