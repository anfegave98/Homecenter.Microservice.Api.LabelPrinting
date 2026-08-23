namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

/// <summary>Derivacion y verificacion de contrasenas.</summary>
public interface IPasswordHasher
{
    /// <summary>Genera hash y salt para una contrasena en texto plano.</summary>
    /// <param name="password">Contrasena a proteger.</param>
    /// <returns>Hash y salt, ambos en Base64.</returns>
    (string Hash, string Salt) Hash(string password);

    /// <summary>Verifica una contrasena contra su hash almacenado.</summary>
    /// <param name="password">Contrasena recibida.</param>
    /// <param name="hash">Hash almacenado.</param>
    /// <param name="salt">Salt almacenado.</param>
    /// <returns>True si la contrasena corresponde.</returns>
    bool Verify(string password, string hash, string salt);
}
