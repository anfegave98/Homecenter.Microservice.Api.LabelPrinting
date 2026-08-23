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

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray() ?? Array.Empty<string>();

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
