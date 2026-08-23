namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;

/// <summary>
/// Usuario autenticado. No transporta hash ni salt: esos campos no salen de la base.
/// </summary>
public sealed class AuthUserDto
{
    /// <summary>Identificador del usuario.</summary>
    public required int Id { get; init; }

    /// <summary>Nombre de usuario.</summary>
    public required string UserName { get; init; }

    /// <summary>Nombre completo para mostrar en la interfaz.</summary>
    public required string FullName { get; init; }

    /// <summary>Roles asignados. Determinan que puede hacer en el submodulo.</summary>
    public required IReadOnlyCollection<string> Roles { get; init; }
}
