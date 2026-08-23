using System.Text.Json;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

namespace Homecenter.Microservice.Api.LabelPrinting.Middleware;

/// <summary>
/// Convierte cualquier excepcion no controlada en una respuesta con el envelope
/// estandar, sin filtrar detalles internos.
///
/// El detalle completo se registra en el log del servidor junto al identificador de
/// correlacion; al cliente solo le llega ese identificador. Asi el soporte puede
/// encontrar el error exacto sin que el mensaje de error revele estructura interna,
/// rutas de archivos ni consultas SQL a quien llame la API.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private const string GenericMessage =
        "Ocurrio un error inesperado al procesar la solicitud. "
        + "Reporte el identificador de correlacion al equipo de soporte.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>Crea el middleware con sus dependencias.</summary>
    /// <param name="next">Siguiente elemento de la tuberia.</param>
    /// <param name="logger">Registro de eventos.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Ejecuta el middleware sobre la solicitud en curso.</summary>
    /// <param name="context">Contexto HTTP de la solicitud.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente cerro la conexion. No es una falla del servicio y registrarlo
            // como error llenaria el log de ruido durante un incidente.
            _logger.LogInformation(
                "Solicitud cancelada por el cliente. Ruta={Ruta} CorrelationId={CorrelationId}",
                context.Request.Path,
                context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;

        _logger.LogError(
            exception,
            "Error no controlado. Ruta={Ruta} Metodo={Metodo} CorrelationId={CorrelationId}",
            context.Request.Path,
            context.Request.Method,
            correlationId);

        if (context.Response.HasStarted)
        {
            // La respuesta ya viaja al cliente: reescribirla produciria una carga
            // corrupta. Queda el log, que es lo unico util a esta altura.
            _logger.LogWarning(
                "La respuesta ya habia iniciado; no se pudo enviar el envelope de error. CorrelationId={CorrelationId}",
                correlationId);
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        context.Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        var payload = ApiResponse<object>.Fail(new ApiError
        {
            Code = "INTERNAL_ERROR",
            Message = GenericMessage,
            Details = new object[] { new { correlationId } }
        });

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
