namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Rol de autorizacion del sistema.
/// </summary>
public class Role : EntityBase
{
    /// <summary>Nombre del rol. Viaja como claim en el token.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Descripcion funcional de lo que habilita el rol.</summary>
    public string? Description { get; set; }

    /// <summary>Usuarios que tienen asignado el rol.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
