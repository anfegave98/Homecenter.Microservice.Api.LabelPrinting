namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Asignacion de un rol a un usuario.
/// </summary>
public class UserRole : EntityBase
{
    /// <summary>Identificador del usuario.</summary>
    public int IdUser { get; set; }

    /// <summary>Identificador del rol.</summary>
    public int IdRole { get; set; }

    /// <summary>Usuario asociado.</summary>
    public User User { get; set; } = null!;

    /// <summary>Rol asociado.</summary>
    public Role Role { get; set; } = null!;
}
