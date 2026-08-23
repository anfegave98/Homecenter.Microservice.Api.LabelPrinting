namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;

/// <summary>
/// Cifrado simetrico de datos sensibles.
///
/// Se expone como abstraccion para que la logica no dependa del algoritmo concreto:
/// rotar de AES a otro esquema no deberia obligar a tocar casos de uso.
/// </summary>
public interface IEncryptionService
{
    /// <summary>Indica si el cifrado esta habilitado en el ambiente actual.</summary>
    bool IsEnabled { get; }

    /// <summary>Cifra un texto plano.</summary>
    /// <param name="plainText">Texto a proteger.</param>
    /// <returns>Texto cifrado en Base64, con su vector de inicializacion incluido.</returns>
    string Encrypt(string plainText);

    /// <summary>Descifra un texto producido por <see cref="Encrypt"/>.</summary>
    /// <param name="cipherText">Texto cifrado en Base64.</param>
    /// <returns>El texto plano original.</returns>
    string Decrypt(string cipherText);

    /// <summary>
    /// Intenta descifrar sin lanzar excepcion.
    ///
    /// Existe porque un payload ilegible casi siempre es un cliente mal configurado y
    /// no una falla del servicio: conviene responder un error de contrato controlado
    /// en vez de un 500.
    /// </summary>
    /// <param name="cipherText">Texto cifrado en Base64.</param>
    /// <param name="plainText">Texto plano resultante, o null si no se pudo descifrar.</param>
    /// <returns>True si el descifrado fue satisfactorio.</returns>
    bool TryDecrypt(string cipherText, out string? plainText);
}
