using System.Security.Cryptography;
using System.Text;
using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Services;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;
using Microsoft.Extensions.Options;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Services;

/// <summary>
/// Cifrado AES-256 en modo CBC con relleno PKCS7.
///
/// **Decision de diseno: el vector de inicializacion es aleatorio por operacion y viaja
/// como prefijo del mensaje, en lugar de tomarse de configuracion.** Un IV fijo en CBC
/// hace que dos textos identicos produzcan exactamente el mismo criptograma, lo que
/// revela cuando dos valores son iguales sin necesidad de descifrarlos. Un IV no es un
/// secreto: solo tiene que ser distinto cada vez, y por eso se transmite junto al mensaje.
///
/// Formato del mensaje: Base64( IV[16] || criptograma ).
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private const int IvLength = 16;

    private readonly byte[] _key;

    /// <summary>Crea el servicio a partir de la configuracion de cifrado.</summary>
    /// <param name="options">Opciones de cifrado del ambiente.</param>
    public AesEncryptionService(IOptions<EncryptionOptions> options)
    {
        var configuration = options.Value;
        IsEnabled = configuration.Enabled;

        // Con el cifrado apagado no se exigen llaves: el arranque no debe fallar por una
        // funcionalidad que el ambiente no usa. La validacion de formato vive en
        // SecretsValidator y se activa junto con el interruptor.
        _key = IsEnabled ? Convert.FromBase64String(configuration.Key) : Array.Empty<byte>();
    }

    /// <inheritdoc />
    public bool IsEnabled { get; }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        EnsureEnabled();
        ArgumentNullException.ThrowIfNull(plainText);

        using var aes = CreateAes();
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var payload = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(payload);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        EnsureEnabled();
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);

        var payload = Convert.FromBase64String(cipherText);

        if (payload.Length <= IvLength)
        {
            throw new CryptographicException(
                "El mensaje cifrado no contiene vector de inicializacion y contenido.");
        }

        var iv = new byte[IvLength];
        Buffer.BlockCopy(payload, 0, iv, 0, IvLength);

        using var aes = CreateAes();
        using var decryptor = aes.CreateDecryptor(aes.Key, iv);

        var plainBytes = decryptor.TransformFinalBlock(payload, IvLength, payload.Length - IvLength);

        // Un texto que no es UTF-8 valido significa llave equivocada: sin esta
        // verificacion se devolveria basura silenciosamente en vez de fallar.
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
            .GetString(plainBytes);
    }

    /// <inheritdoc />
    public bool TryDecrypt(string cipherText, out string? plainText)
    {
        plainText = null;

        if (!IsEnabled || string.IsNullOrWhiteSpace(cipherText))
        {
            return false;
        }

        try
        {
            plainText = Decrypt(cipherText);
            return true;
        }
        catch (Exception exception) when (
            exception is FormatException or CryptographicException or ArgumentException or DecoderFallbackException)
        {
            // Un payload ilegible es un cliente mal configurado, no una falla del
            // servicio. Se informa el fallo y quien llama decide como responderlo.
            return false;
        }
    }

    private Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        return aes;
    }

    private void EnsureEnabled()
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException(
                "El cifrado esta deshabilitado en este ambiente (Encryption:Enabled = false).");
        }
    }
}
