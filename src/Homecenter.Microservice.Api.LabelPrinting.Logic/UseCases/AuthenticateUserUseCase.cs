using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;

/// <summary>
/// Autentica un usuario operativo y emite su token.
///
/// El mensaje de error es identico para usuario inexistente y contrasena incorrecta:
/// distinguirlos permitiria enumerar usuarios validos del sistema.
/// </summary>
public sealed class AuthenticateUserUseCase : IAuthenticateUserUseCase
{
    public const string InvalidCredentialsCode = "INVALID_CREDENTIALS";
    public const string InactiveUserCode = "USER_INACTIVE";

    private const string InvalidCredentialsMessage = "Usuario o contrasena incorrectos.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthenticateUserUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<ApiResponse<LoginResponseDto>> ExecuteAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUserNameAsync(request.UserName, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return ApiResponse<LoginResponseDto>.Fail(InvalidCredentialsCode, InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            return ApiResponse<LoginResponseDto>.Fail(
                InactiveUserCode,
                "El usuario se encuentra inactivo. Contacte al administrador.");
        }

        var roles = user.UserRoles
                        .Where(x => x.State && x.Role is not null)
                        .Select(x => x.Role.Name)
                        .Distinct()
                        .ToArray();

        var (token, expiresIn) = _tokenGenerator.Generate(user, roles);

        await _userRepository.UpdateLastLoginAsync(user.Id, DateTimeOffset.UtcNow, cancellationToken);

        var response = new LoginResponseDto
        {
            AccessToken = token,
            ExpiresIn = expiresIn,
            User = new AuthUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Roles = roles
            }
        };

        return ApiResponse<LoginResponseDto>.Ok(response);
    }
}
