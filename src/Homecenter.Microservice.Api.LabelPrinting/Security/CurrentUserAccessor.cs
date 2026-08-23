using System.Security.Claims;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

namespace Homecenter.Microservice.Api.LabelPrinting.Security;

/// <summary>
/// Lee la identidad del usuario desde los claims del token.
/// La logica de negocio depende de esta abstraccion y no de HttpContext,
/// para poder probar las reglas sin levantar un servidor.
/// </summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Crea el accesor sobre el contexto HTTP actual.</summary>
    /// <param name="httpContextAccessor">Accesor al contexto de la peticion.</param>
    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <inheritdoc />
    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray() ?? Array.Empty<string>();

    /// <inheritdoc />
    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
