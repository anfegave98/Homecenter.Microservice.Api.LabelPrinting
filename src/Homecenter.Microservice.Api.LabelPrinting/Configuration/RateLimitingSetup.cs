using System.Text.Json;
using System.Threading.RateLimiting;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;
using Homecenter.Microservice.Api.LabelPrinting.Middleware;
using Microsoft.AspNetCore.RateLimiting;

namespace Homecenter.Microservice.Api.LabelPrinting.Configuration;

/// <summary>
/// Politicas de limitacion de solicitudes.
///
/// El limite se reparte por usuario autenticado y, para lo anonimo, por IP. Un limite
/// global unico seria inutil en una tienda: un solo operario con el dedo pegado al
/// boton dejaria sin servicio a los demas.
/// </summary>
public static class RateLimitingSetup
{
    /// <summary>Politica del endpoint de autenticacion.</summary>
    public const string AuthPolicy = "AuthEndpoints";

    /// <summary>Politica del endpoint transaccional de impresion.</summary>
    public const string PrintingPolicy = "PrintingEndpoints";

    /// <summary>Politica de los endpoints de consulta.</summary>
    public const string QueryPolicy = "QueryEndpoints";

    private const string RejectionCode = "TOO_MANY_REQUESTS";
    private const string AnonymousPartition = "anon:";
    private const string UserPartition = "user:";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Registra el limitador con sus politicas configuradas.</summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <param name="options">Limites leidos de configuracion.</param>
    /// <returns>La misma coleccion, para encadenar.</returns>
    public static IServiceCollection AddConfiguredRateLimiting(
        this IServiceCollection services,
        RateLimitingOptions options)
    {
        if (!options.Enabled)
        {
            // El interruptor existe para poder desactivarlo en una demostracion o ante
            // una calibracion equivocada en produccion, sin recompilar.
            return services;
        }

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = options.RejectedStatusCode;

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                BuildPartition(context, ResolvePartitionKey(context, options), options.PermitLimit, options.WindowSeconds, options.QueueLimit));

            AddPolicy(limiter, AuthPolicy, options);
            AddPolicy(limiter, PrintingPolicy, options);
            AddPolicy(limiter, QueryPolicy, options);

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting");

                logger.LogWarning(
                    "Solicitud rechazada por limite de trafico. Ruta={Ruta} Particion={Particion} CorrelationId={CorrelationId}",
                    context.HttpContext.Request.Path,
                    ResolvePartitionKey(context.HttpContext, options),
                    context.HttpContext.TraceIdentifier);

                var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? (int)retryAfter.TotalSeconds
                    : options.WindowSeconds;

                // Se informa cuanto esperar: sin esa referencia el cliente reintenta a
                // ciegas y agrava justamente la rafaga que se esta conteniendo.
                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName] =
                    context.HttpContext.TraceIdentifier;

                var payload = ApiResponse<object>.Fail(new ApiError
                {
                    Code = RejectionCode,
                    Message = $"Se superó el límite de solicitudes permitido. Reintente en {retryAfterSeconds} segundos.",
                    Details = new object[] { new { retryAfterSeconds } }
                });

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(payload, SerializerOptions),
                    cancellationToken);
            };
        });

        return services;
    }

    private static void AddPolicy(RateLimiterOptions limiter, string policyName, RateLimitingOptions options)
    {
        // Una politica sin configuracion propia hereda el limite general en vez de
        // quedar sin limite: omitir la seccion no debe deshabilitar la proteccion.
        var policy = options.Policies.TryGetValue(policyName, out var configured)
            ? configured
            : new RateLimitPolicyOptions { PermitLimit = options.PermitLimit, WindowSeconds = options.WindowSeconds };

        limiter.AddPolicy(policyName, context =>
            BuildPartition(
                context,
                $"{policyName}|{ResolvePartitionKey(context, options)}",
                policy.PermitLimit,
                policy.WindowSeconds,
                options.QueueLimit));
    }

    private static RateLimitPartition<string> BuildPartition(
        HttpContext context,
        string partitionKey,
        int permitLimit,
        int windowSeconds,
        int queueLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = queueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

    /// <summary>
    /// Resuelve la particion contra la que se cuenta la solicitud.
    ///
    /// Un usuario autenticado se cuenta por su identidad y no por su IP: en una tienda
    /// varios operarios comparten la misma salida a internet, y contarlos juntos haria
    /// que uno agotara el cupo de todos.
    /// </summary>
    private static string ResolvePartitionKey(HttpContext context, RateLimitingOptions options)
    {
        if (options.ApplyByAuthenticatedUser
            && context.User.Identity?.IsAuthenticated == true
            && !string.IsNullOrWhiteSpace(context.User.Identity.Name))
        {
            return UserPartition + context.User.Identity.Name;
        }

        if (options.ApplyByIpForAnonymous)
        {
            return AnonymousPartition + (context.Connection.RemoteIpAddress?.ToString() ?? "desconocida");
        }

        return "global";
    }
}
