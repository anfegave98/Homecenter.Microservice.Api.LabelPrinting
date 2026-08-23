using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

public interface IAuthenticateUserUseCase
{
    Task<ApiResponse<LoginResponseDto>> ExecuteAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
