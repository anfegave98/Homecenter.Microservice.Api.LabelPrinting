namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

public interface IPasswordHasher
{
    /// <summary>Genera hash y salt para una contrasena en texto plano.</summary>
    (string Hash, string Salt) Hash(string password);

    /// <summary>Verifica una contrasena contra su hash almacenado.</summary>
    bool Verify(string password, string hash, string salt);
}
