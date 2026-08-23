namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Usuario operativo. La contrasena se persiste como hash + salt;
/// nunca se expone en DTOs ni viaja al frontend.
/// </summary>
public class User : EntityBase
{
    /// <summary>Nombre de usuario. Es la llave de autenticacion y debe ser unico.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Nombre completo mostrado en la interfaz y en el historial.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 de la contrasena, en Base64.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Salt aleatorio por usuario, en Base64.</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>Indica si el usuario puede autenticarse.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Fecha del ultimo inicio de sesion exitoso, en UTC.</summary>
    public DateTimeOffset? LastLoginDate { get; set; }

    /// <summary>Roles asignados al usuario.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
