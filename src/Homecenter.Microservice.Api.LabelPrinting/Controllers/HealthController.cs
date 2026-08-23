using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Homecenter.Microservice.Api.LabelPrinting.Controllers;

/// <summary>
/// Health check tecnico. Es el primer punto que se consulta ante un incidente productivo
/// y el endpoint que Render usa para determinar si la instancia esta viva.
/// </summary>
[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private const string ServiceName = "Homecenter.Microservice.Api.LabelPrinting";

    private readonly HealthCheckService _healthCheckService;

    /// <summary>Crea el controlador con el servicio de health checks.</summary>
    /// <param name="healthCheckService">Servicio que agrega los chequeos registrados.</param>
    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>Consulta el estado del servicio y de sus dependencias.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Estado agregado; 503 si algun componente no responde.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HealthStatusResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
        var databaseEntry = report.Entries.TryGetValue("database", out var entry) ? entry.Status.ToString() : "NotConfigured";

        var payload = new HealthStatusResponse
        {
            Status = report.Status.ToString(),
            Service = ServiceName,
            Database = databaseEntry,
            Timestamp = DateTimeOffset.UtcNow
        };

        var isHealthy = report.Status == HealthStatus.Healthy;
        var response = isHealthy
            ? ApiResponse<HealthStatusResponse>.Ok(payload)
            : ApiResponse<HealthStatusResponse>.Fail(
                new ApiError { Code = "SERVICE_UNHEALTHY", Message = "Uno o mas componentes no estan disponibles." },
                payload);

        return StatusCode(isHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable, response);
    }
}

/// <summary>Componente HealthStatusResponse del submodulo de impresion.</summary>
public sealed class HealthStatusResponse
{
    /// <summary>Estado agregado del servicio.</summary>
    public required string Status { get; init; }
    /// <summary>Nombre del microservicio.</summary>
    public required string Service { get; init; }
    /// <summary>Estado de la conexion a PostgreSQL.</summary>
    public required string Database { get; init; }
    /// <summary>Momento de la verificacion, en UTC.</summary>
    public required DateTimeOffset Timestamp { get; init; }
}
