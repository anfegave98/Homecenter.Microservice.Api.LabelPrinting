namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

/// <summary>
/// Identidad del usuario autenticado, tomada del token.
/// Aisla a la logica de negocio de HttpContext: las reglas no deben conocer el transporte.
/// </summary>
public interface ICurrentUserAccessor
{
    int? UserId { get; }

    string? UserName { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsInRole(string role);
}
