using System.Text.Json;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Configuration;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Homecenter.Microservice.Api.LabelPrinting.Controllers;

/// <summary>Componente AuthController del submodulo de impresion.</summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting(RateLimitingSetup.AuthPolicy)]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private const string InvalidPayloadCode = "INVALID_ENCRYPTED_PAYLOAD";
    private const string MissingCredentialsCode = "MISSING_CREDENTIALS";

    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAuthenticateUserUseCase _authenticateUser;
    private readonly IEncryptionService _encryption;

    /// <summary>Crea el controlador con sus dependencias.</summary>
    /// <param name="authenticateUser">Caso de uso de autenticacion.</param>
    /// <param name="encryption">Servicio de cifrado del payload sensible.</param>
    public AuthController(IAuthenticateUserUseCase authenticateUser, IEncryptionService encryption)
    {
        _authenticateUser = authenticateUser;
        _encryption = encryption;
    }

    /// <summary>Autentica un usuario operativo y retorna su token de acceso.</summary>
    /// <remarks>
    /// Acepta las credenciales en claro o dentro de `encryptedPayload` (AES-256-CBC en
    /// Base64). El cifrado del payload es una capa adicional sobre HTTPS, no un
    /// reemplazo: la confidencialidad real en transito la aporta TLS.
    /// </remarks>
    /// <param name="request">Credenciales, en claro o cifradas.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Token de acceso, o el motivo del rechazo.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryResolveCredentials(request, out var credentials, out var error))
        {
            return BadRequest(ApiResponse<LoginResponseDto>.Fail(error!));
        }

        var result = await _authenticateUser.ExecuteAsync(credentials!, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }

        // Un fallo de autenticacion si es un error tecnico de acceso, no una decision
        // de negocio: por eso aqui si se usan 401/403 y no el 200 del envelope.
        var statusCode = result.Error?.Code == AuthenticateUserUseCase.InactiveUserCode
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;

        return StatusCode(statusCode, result);
    }

    /// <summary>
    /// Resuelve las credenciales efectivas, descifrando el payload si viene informado.
    ///
    /// La validacion vive aqui y no en atributos porque el DTO admite dos formas
    /// excluyentes: exigir los campos en claro con [Required] rechazaria una solicitud
    /// cifrada perfectamente valida.
    /// </summary>
    private bool TryResolveCredentials(
        LoginRequestDto request,
        out LoginRequestDto? credentials,
        out ApiError? error)
    {
        credentials = null;
        error = null;

        if (!string.IsNullOrWhiteSpace(request.EncryptedPayload))
        {
            if (!_encryption.IsEnabled)
            {
                error = new ApiError
                {
                    Code = InvalidPayloadCode,
                    Message = "El servicio no tiene habilitado el cifrado de credenciales."
                };
                return false;
            }

            if (!_encryption.TryDecrypt(request.EncryptedPayload, out var plainText))
            {
                // Payload ilegible es cliente mal configurado, no falla del servicio:
                // se responde 400 controlado y no se filtra el motivo criptografico.
                error = new ApiError
                {
                    Code = InvalidPayloadCode,
                    Message = "No fue posible descifrar las credenciales enviadas."
                };
                return false;
            }

            credentials = Deserialize(plainText!);

            if (credentials is null)
            {
                error = new ApiError
                {
                    Code = InvalidPayloadCode,
                    Message = "Las credenciales descifradas no tienen el formato esperado."
                };
                return false;
            }
        }
        else
        {
            credentials = request;
        }

        if (string.IsNullOrWhiteSpace(credentials.UserName) || string.IsNullOrWhiteSpace(credentials.Password))
        {
            error = new ApiError
            {
                Code = MissingCredentialsCode,
                Message = "El usuario y la contrasena son obligatorios."
            };
            return false;
        }

        return true;
    }

    private static LoginRequestDto? Deserialize(string plainText)
    {
        try
        {
            return JsonSerializer.Deserialize<LoginRequestDto>(plainText, PayloadOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
