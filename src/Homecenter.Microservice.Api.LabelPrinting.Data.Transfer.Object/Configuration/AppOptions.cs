namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;

/// <summary>
/// Opciones tipadas de la aplicacion. Todo valor que cambie entre ambientes vive aqui,
/// nunca quemado en codigo. Los valores sensibles se inyectan por variable de entorno.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    public bool Enabled { get; set; }
    public string Algorithm { get; set; } = "AES";
    public string Key { get; set; } = string.Empty;
    public string IV { get; set; } = string.Empty;
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; }
    public bool ApplyByAuthenticatedUser { get; set; } = true;
    public bool ApplyByIpForAnonymous { get; set; } = true;
    public int RejectedStatusCode { get; set; } = 429;
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new();
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Origenes autorizados. El frontend se despliega en Cloudflare Pages y el API en Render:
    /// al vivir en dominios distintos, un origen faltante aqui rompe la aplicacion completa.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public sealed class PrintingOptions
{
    public const string SectionName = "Printing";

    public string SimulationMode { get; set; } = "LogicalEvent";
    public string OutputDirectory { get; set; } = "./output/zpl";
    public bool PersistZplFile { get; set; } = true;
}

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; } = true;
    public string MocksPath { get; set; } = "./mocks";
}

public sealed class SwaggerOptions
{
    public const string SectionName = "Swagger";

    public bool Enabled { get; set; } = true;
}
