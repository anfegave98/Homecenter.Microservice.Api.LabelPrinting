using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homecenter.Microservice.Api.LabelPrinting.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticateUserUseCase _authenticateUser;

    public AuthController(IAuthenticateUserUseCase authenticateUser)
    {
        _authenticateUser = authenticateUser;
    }

    /// <summary>Autentica un usuario operativo y retorna su token de acceso.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authenticateUser.ExecuteAsync(request, cancellationToken);

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
}
