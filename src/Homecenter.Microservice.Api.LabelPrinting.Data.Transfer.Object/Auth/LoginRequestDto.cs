using System.ComponentModel.DataAnnotations;

namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;

/// <summary>
/// Credenciales de acceso al submodulo.
///
/// Admite dos formas: los campos en claro, o <see cref="EncryptedPayload"/> con el
/// mismo objeto cifrado. La segunda se habilita con Encryption:Enabled.
/// </summary>
public sealed class LoginRequestDto
{
    /// <summary>Nombre de usuario operativo.</summary>
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Contrasena. Solo viaja sobre HTTPS y no se registra en logs.</summary>
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Credenciales cifradas en Base64: AES-256-CBC de {"userName","password"} con el
    /// IV aleatorio como prefijo. Cuando viene informado, prevalece sobre los campos en
    /// claro. Los campos dejaron de ser [Required] porque una solicitud valida puede
    /// traer solo este.
    /// </summary>
    [MaxLength(2000)]
    public string? EncryptedPayload { get; set; }
}
