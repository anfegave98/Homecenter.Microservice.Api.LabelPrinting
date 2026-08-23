namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;

/// <summary>
/// Usuario autenticado. No transporta hash ni salt: esos campos no salen de la base.
/// </summary>
public sealed class AuthUserDto
{
    public required int Id { get; init; }

    public required string UserName { get; init; }

    public required string FullName { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }
}
