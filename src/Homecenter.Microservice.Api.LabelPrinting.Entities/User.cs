namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Usuario operativo. La contrasena se persiste como hash + salt;
/// nunca se expone en DTOs ni viaja al frontend.
/// </summary>
public class User : EntityBase
{
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginDate { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
