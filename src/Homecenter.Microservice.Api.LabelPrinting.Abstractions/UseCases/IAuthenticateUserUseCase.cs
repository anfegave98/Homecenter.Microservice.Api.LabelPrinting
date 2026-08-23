using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

/// <summary>Autenticacion de usuarios operativos.</summary>
public interface IAuthenticateUserUseCase
{
    /// <summary>Valida credenciales y emite el token de acceso.</summary>
    /// <param name="request">Credenciales recibidas.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Token y datos del usuario, o el error de autenticacion.</returns>
    Task<ApiResponse<LoginResponseDto>> ExecuteAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
