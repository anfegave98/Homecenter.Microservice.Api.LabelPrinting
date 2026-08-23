using Npgsql;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Configuration;

/// <summary>
/// Normaliza la cadena de conexion a la forma que entiende Npgsql.
///
/// Render —y la mayoria de plataformas gestionadas— entregan la conexion como URI
/// (`postgresql://usuario:clave@host:puerto/base`). Npgsql **no** acepta ese formato:
/// espera pares clave=valor. Sin esta conversion el servicio arranca y falla al primer
/// acceso a datos con un error de formato que no dice nada sobre la causa real.
///
/// Se hace aqui y no en el panel para que enlazar la base desde la plataforma —que es
/// lo correcto, porque rota la credencial sola— no obligue a copiar la cadena a mano.
/// </summary>
public static class ConnectionStringNormalizer
{
    private static readonly string[] UriSchemes = { "postgres://", "postgresql://" };

    /// <summary>Convierte la cadena recibida al formato clave=valor de Npgsql.</summary>
    /// <param name="connectionString">Cadena en formato URI o clave=valor.</param>
    /// <returns>Cadena lista para <c>UseNpgsql</c>; la entrada tal cual si ya lo estaba.</returns>
    public static string Normalize(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var trimmed = connectionString.Trim();

        // Ya viene en clave=valor: no se toca. Reescribirla podria perder ajustes
        // que alguien puso a proposito (timeouts, pooling, certificados).
        if (!UriSchemes.Any(scheme => trimmed.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)))
        {
            return trimmed;
        }

        var uri = new Uri(trimmed);
        var credenciales = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(credenciales[0]),
            Password = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : string.Empty,

            // Render exige TLS. En Npgsql 8, Require cifra sin validar el certificado,
            // que es lo que corresponde aqui: lo emite la autoridad interna de Render y no
            // esta en el almacen de confianza de la imagen base. VerifyCA fallaria sin que
            // haya nada que corregir del lado de la aplicacion.
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
