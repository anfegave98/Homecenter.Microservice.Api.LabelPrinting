namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;

/// <summary>Token de acceso emitido tras una autenticacion satisfactoria.</summary>
public sealed class LoginResponseDto
{
    /// <summary>Token JWT firmado, con el usuario y sus roles como claims.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Esquema de autorizacion a usar en la cabecera.</summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>Vigencia del token en segundos.</summary>
    public required int ExpiresIn { get; init; }

    /// <summary>Datos minimos del usuario autenticado.</summary>
    public required AuthUserDto User { get; init; }
}
