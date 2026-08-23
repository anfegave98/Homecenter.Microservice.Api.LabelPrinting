namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

/// <summary>
/// Identidad del usuario autenticado, tomada del token.
/// Aisla a la logica de negocio de HttpContext: las reglas no deben conocer el transporte.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>Identificador del usuario autenticado, o null si no hay sesion.</summary>
    int? UserId { get; }

    /// <summary>Nombre del usuario autenticado, o null si no hay sesion.</summary>
    string? UserName { get; }

    /// <summary>Roles del usuario autenticado.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Indica si el usuario tiene el rol indicado.</summary>
    /// <param name="role">Nombre del rol a verificar.</param>
    /// <returns>True si el usuario posee ese rol.</returns>
    bool IsInRole(string role);
}
