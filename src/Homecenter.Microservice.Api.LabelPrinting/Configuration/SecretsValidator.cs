using System.Text;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;

namespace Homecenter.Microservice.Api.LabelPrinting.Configuration;

/// <summary>
/// Verifica al arrancar que las claves criptograficas y la cadena de conexion sean
/// utilizables, y detiene el servicio si no lo son.
///
/// El riesgo real no es olvidar una clave: es desplegar con el marcador de posicion
/// puesto y no enterarse. Un servicio que arranca firmando tokens con
/// "CHANGE_ME_FROM_ENVIRONMENT" acepta cualquier token que firme quien conozca ese
/// texto, y nada en el log lo delata. Por eso esto falla ruidosamente al inicio y no
/// silenciosamente en la primera peticion.
/// </summary>
public static class SecretsValidator
{
    /// <summary>Marcador que traen los archivos versionados en lugar de un valor real.</summary>
    private const string Placeholder = "CHANGE_ME_FROM_ENVIRONMENT";

    /// <summary>Longitud minima de la llave de firma, en bytes (HMAC-SHA256).</summary>
    private const int MinimumJwtKeyBytes = 32;

    /// <summary>Longitud exacta de la llave AES-256, en bytes.</summary>
    private const int AesKeyBytes = 32;

    /// <summary>Longitud exacta del vector de inicializacion AES, en bytes.</summary>
    private const int AesBlockBytes = 16;

    /// <summary>
    /// Valida la configuracion sensible y lanza si algo impide operar de forma segura.
    /// </summary>
    /// <param name="jwt">Opciones de firma del token.</param>
    /// <param name="encryption">Opciones de cifrado simetrico.</param>
    /// <param name="connectionString">Cadena de conexion a PostgreSQL.</param>
    /// <exception cref="InvalidOperationException">
    /// Si falta un valor obligatorio, quedo el marcador de posicion o la llave no tiene
    /// la longitud que exige el algoritmo.
    /// </exception>
    public static void Validate(JwtOptions jwt, EncryptionOptions encryption, string connectionString)
    {
        var failures = new List<string>();

        ValidateConnectionString(connectionString, failures);
        ValidateJwt(jwt, failures);
        ValidateEncryption(encryption, failures);

        if (failures.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "La configuracion sensible no es valida y el servicio no puede iniciar:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, failures.Select(failure => $"  - {failure}"))
            + Environment.NewLine
            + "Define los valores como variables de entorno. Ver docs/SECRETS.md.");
    }

    private static void ValidateConnectionString(string connectionString, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            failures.Add("ConnectionStrings:DefaultConnection esta vacia.");
        }
    }

    private static void ValidateJwt(JwtOptions jwt, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
        {
            failures.Add("Jwt:Issuer y Jwt:Audience son obligatorios.");
        }

        if (jwt.ExpirationMinutes <= 0)
        {
            failures.Add("Jwt:ExpirationMinutes debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(jwt.SecretKey) || jwt.SecretKey == Placeholder)
        {
            failures.Add("Jwt:SecretKey no esta definida (conserva el marcador de posicion).");
            return;
        }

        // Se mide en bytes UTF-8 porque asi es como la consume JwtTokenGenerator al
        // construir la SymmetricSecurityKey. Medir caracteres daria un falso positivo.
        var keyBytes = Encoding.UTF8.GetByteCount(jwt.SecretKey);
        if (keyBytes < MinimumJwtKeyBytes)
        {
            failures.Add(
                $"Jwt:SecretKey tiene {keyBytes} bytes; HMAC-SHA256 exige al menos {MinimumJwtKeyBytes}.");
        }
    }

    private static void ValidateEncryption(EncryptionOptions encryption, List<string> failures)
    {
        // Si el cifrado esta apagado, sus llaves no participan en ninguna operacion:
        // exigirlas seria bloquear el arranque por una funcionalidad que no se usa.
        if (!encryption.Enabled)
        {
            return;
        }

        if (!string.Equals(encryption.Algorithm, "AES", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"Encryption:Algorithm '{encryption.Algorithm}' no esta soportado. Use AES.");
        }

        ValidateBase64Key(encryption.Key, "Encryption:Key", AesKeyBytes, failures);

        // Encryption:IV ya NO se exige: AesEncryptionService genera un vector de
        // inicializacion aleatorio por operacion y lo transmite junto al mensaje, porque
        // un IV fijo en CBC delata cuando dos textos son iguales. El ajuste se conserva
        // por compatibilidad de configuracion y solo se valida su formato si alguien lo
        // define, para que un valor equivocado no quede ahi aparentando estar en uso.
        if (!string.IsNullOrWhiteSpace(encryption.IV) && encryption.IV != Placeholder)
        {
            ValidateBase64Key(encryption.IV, "Encryption:IV", AesBlockBytes, failures);
        }
    }

    private static void ValidateBase64Key(string value, string setting, int expectedBytes, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value) || value == Placeholder)
        {
            failures.Add($"{setting} no esta definida (conserva el marcador de posicion).");
            return;
        }

        if (!TryDecodeBase64(value, out var decodedLength))
        {
            failures.Add($"{setting} no es Base64 valido.");
            return;
        }

        if (decodedLength != expectedBytes)
        {
            failures.Add(
                $"{setting} decodifica a {decodedLength} bytes; se esperan exactamente {expectedBytes}.");
        }
    }

    private static bool TryDecodeBase64(string value, out int decodedLength)
    {
        var buffer = new Span<byte>(new byte[value.Length]);
        if (Convert.TryFromBase64String(value, buffer, out decodedLength))
        {
            return true;
        }

        decodedLength = 0;
        return false;
    }
}
