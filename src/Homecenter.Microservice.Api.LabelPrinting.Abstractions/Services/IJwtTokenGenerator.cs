using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

/// <summary>Emision del token de acceso.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Emite un token firmado con el usuario y sus roles como claims.</summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <param name="roles">Roles asignados al usuario.</param>
    /// <returns>Token firmado y su vigencia en segundos.</returns>
    (string Token, int ExpiresInSeconds) Generate(User user, IReadOnlyCollection<string> roles);
}
