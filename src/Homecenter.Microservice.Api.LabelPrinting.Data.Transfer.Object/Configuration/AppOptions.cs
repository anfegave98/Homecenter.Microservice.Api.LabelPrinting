namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;

/// <summary>
/// Parametros de emision y validacion del token de acceso.
///
/// Es una de las opciones tipadas de la aplicacion: todo valor que cambie entre
/// ambientes vive en configuracion, nunca quemado en codigo. La clave de firma se
/// inyecta por variable de entorno y no se versiona.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Nombre de la seccion en appsettings.json.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Emisor del token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Audiencia autorizada del token.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Clave de firma simetrica. Se consume como bytes UTF-8 y debe tener al menos
    /// 32 bytes, que es lo que exige HMAC-SHA256. El arranque falla si no los cumple.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Vigencia del token en minutos.</summary>
    public int ExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// Parametros de cifrado de datos sensibles. Se habilita por ambiente.
/// </summary>
public sealed class EncryptionOptions
{
    /// <summary>Nombre de la seccion en appsettings.json.</summary>
    public const string SectionName = "Encryption";

    /// <summary>Habilita el cifrado de datos sensibles.</summary>
    public bool Enabled { get; set; }

    /// <summary>Algoritmo de cifrado aplicado.</summary>
    public string Algorithm { get; set; } = "AES";

    /// <summary>
    /// Llave de cifrado en Base64. Debe decodificar a exactamente 32 bytes (AES-256).
    /// Se inyecta por variable de entorno y se valida al arrancar.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Vector de inicializacion en Base64. **No se usa para cifrar.** AesEncryptionService
    /// genera un IV aleatorio por operacion y lo transmite como prefijo del mensaje: un IV
    /// fijo en CBC hace que dos textos identicos produzcan el mismo criptograma. El ajuste
    /// se conserva por compatibilidad y, si se define, debe decodificar a 16 bytes.
    /// </summary>
    public string IV { get; set; } = string.Empty;
}

/// <summary>
/// Limites de solicitudes. Se ajustan sin recompilar para poder reaccionar
/// ante un pico de trafico en produccion.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>Nombre de la seccion en appsettings.json.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Habilita la limitacion de solicitudes.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Solicitudes permitidas dentro de la ventana, por defecto.</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>Duracion de la ventana de conteo, en segundos.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Solicitudes que pueden encolarse al superar el limite.</summary>
    public int QueueLimit { get; set; }

    /// <summary>Aplica el limite por usuario autenticado.</summary>
    public bool ApplyByAuthenticatedUser { get; set; } = true;

    /// <summary>Aplica el limite por IP en endpoints publicos.</summary>
    public bool ApplyByIpForAnonymous { get; set; } = true;

    /// <summary>Codigo HTTP devuelto al exceder el limite.</summary>
    public int RejectedStatusCode { get; set; } = 429;

    /// <summary>Politicas especificas por grupo de endpoints.</summary>
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new();
}

/// <summary>Limite aplicable a un grupo de endpoints.</summary>
public sealed class RateLimitPolicyOptions
{
    /// <summary>Solicitudes permitidas dentro de la ventana.</summary>
    public int PermitLimit { get; set; }

    /// <summary>Duracion de la ventana de conteo, en segundos.</summary>
    public int WindowSeconds { get; set; }
}

/// <summary>Origenes autorizados para consumir la API desde un navegador.</summary>
public sealed class CorsOptions
{
    /// <summary>Nombre de la seccion en appsettings.json.</summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Origenes autorizados. El frontend se despliega en Cloudflare Pages y el API en Render:
    /// al vivir en dominios distintos, un origen faltante aqui rompe la aplicacion completa.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

/// <summary>Comportamiento de la simulacion de impresion.</summary>
public sealed class PrintingOptions
{
    /// <summary>Nombre de la seccion en appsettings.json.</summary>
    public const string SectionName = "Printing";

    /// <summary>Modo de simulacion aplicado.</summary>
    public string SimulationMode { get; set; } = "LogicalEvent";

    /// <summary>Carpeta donde se deja el archivo ZPL de evidencia.</summary>
    public string OutputDirectory { get; set; } = "./output/zpl";

    /// <summary>Indica si se escribe el archivo ZPL de salida.</summary>
    public bool PersistZplFile { get; set; } = true;
}

/// <summary>Carga de datos semilla al arranque.</summary>
public sealed class SeedOptions
{
    /// <summary>Nombre de la seccion en appsettings.json.</summary>
    public const string SectionName = "Seed";

    /// <summary>Habilita la carga de datos mock.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Ruta de la carpeta con los archivos mock de semilla.</summary>
    public string MocksPath { get; set; } = "./mocks";
}

/// <summary>Exposicion de la documentacion interactiva de la API.</summary>
public sealed class SwaggerOptions
{
    /// <summary>Nombre de la seccion en appsettings.json.</summary>
    public const string SectionName = "Swagger";

    /// <summary>Habilita Swagger en el ambiente actual.</summary>
    public bool Enabled { get; set; } = true;
}
