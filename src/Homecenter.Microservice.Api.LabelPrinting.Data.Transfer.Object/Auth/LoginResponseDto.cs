namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Auth;

public sealed class LoginResponseDto
{
    public required string AccessToken { get; init; }

    public string TokenType { get; init; } = "Bearer";

    public required int ExpiresIn { get; init; }

    public required AuthUserDto User { get; init; }
}
