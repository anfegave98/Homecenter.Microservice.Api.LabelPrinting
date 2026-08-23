using System.ComponentModel.DataAnnotations;

namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;

/// <summary>Credenciales de acceso al submodulo.</summary>
public sealed class LoginRequestDto
{
    /// <summary>Nombre de usuario operativo.</summary>
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Contrasena en texto plano. Solo viaja sobre HTTPS y no se registra en logs.</summary>
    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
