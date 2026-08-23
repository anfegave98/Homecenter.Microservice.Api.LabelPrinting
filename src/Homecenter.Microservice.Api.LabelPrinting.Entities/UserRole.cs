namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

public class UserRole : EntityBase
{
    public int IdUser { get; set; }

    public int IdRole { get; set; }

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;
}
