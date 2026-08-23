using System.Security.Cryptography;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Security;

/// <summary>
/// Hash de contrasenas con PBKDF2-SHA256.
///
/// Se usa salt aleatorio por usuario para que dos contrasenas iguales produzcan
/// hashes distintos, y comparacion en tiempo constante para no filtrar informacion
/// por el tiempo de respuesta.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <inheritdoc />
    public (string Hash, string Salt) Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return (Convert.ToBase64String(key), Convert.ToBase64String(salt));
    }

    /// <inheritdoc />
    public bool Verify(string password, string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
        {
            return false;
        }

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var expected = Convert.FromBase64String(hash);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, Algorithm, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            // Hash o salt corruptos en base: se trata como credencial invalida,
            // nunca como excepcion que llegue al consumidor.
            return false;
        }
    }
}
