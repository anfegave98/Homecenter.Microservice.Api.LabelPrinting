namespace Homecenter.Microservice.Api.LabelPrinting.Middleware;

/// <summary>
/// Asigna un identificador de correlacion a cada solicitud y lo propaga al log y a
/// la respuesta.
///
/// Es la llave con la que se diagnostica un incidente: el operario reporta el
/// identificador que vio en pantalla y con el se recuperan todas las entradas de log
/// de esa solicitud, sin tener que reconstruir el caso por hora aproximada.
///
/// Si el cliente ya envia uno se respeta, para poder rastrear una operacion que
/// atraviesa varios servicios.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>Nombre del header que transporta el identificador.</summary>
    public const string HeaderName = "X-Correlation-Id";

    private const int MaxLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>Crea el middleware con sus dependencias.</summary>
    /// <param name="next">Siguiente elemento de la tuberia.</param>
    /// <param name="logger">Registro de eventos.</param>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Ejecuta el middleware sobre la solicitud en curso.</summary>
    /// <param name="context">Contexto HTTP de la solicitud.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[HeaderName] = correlationId;
        context.TraceIdentifier = correlationId;

        // Se escribe antes de ejecutar la tuberia: si la respuesta ya empezo a
        // enviarse, agregar headers lanza excepcion.
        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// Toma el identificador del cliente si viene, o genera uno nuevo.
    /// Un valor entrante desmedido se descarta: el header viaja al log y no se acepta
    /// texto arbitrario de longitud libre en el.
    /// </summary>
    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var incoming))
        {
            var candidate = incoming.ToString();

            if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= MaxLength)
            {
                return candidate;
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
