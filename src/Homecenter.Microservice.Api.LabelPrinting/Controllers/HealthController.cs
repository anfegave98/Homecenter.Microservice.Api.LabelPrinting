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

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

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

public sealed class HealthStatusResponse
{
    public required string Status { get; init; }
    public required string Service { get; init; }
    public required string Database { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
