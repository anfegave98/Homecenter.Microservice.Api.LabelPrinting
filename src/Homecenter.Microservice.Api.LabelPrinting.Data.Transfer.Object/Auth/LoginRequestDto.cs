using System.ComponentModel.DataAnnotations;

namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;

public sealed class LoginRequestDto
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
