using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

public interface IJwtTokenGenerator
{
    /// <summary>Emite un token firmado con el usuario y sus roles como claims.</summary>
    (string Token, int ExpiresInSeconds) Generate(User user, IReadOnlyCollection<string> roles);
}
