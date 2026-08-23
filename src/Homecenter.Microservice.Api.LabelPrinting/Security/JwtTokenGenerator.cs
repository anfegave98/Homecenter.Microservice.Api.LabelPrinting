using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Homecenter.Microservice.Api.LabelPrinting.Security;

/// <summary>
/// Emite el token de acceso. Los roles viajan como claims para que la autorizacion
/// del backend no dependa de nada que el cliente pueda manipular.
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, int ExpiresInSeconds) Generate(User user, IReadOnlyCollection<string> roles)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("fullName", user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expires = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), _options.ExpirationMinutes * 60);
    }
}
