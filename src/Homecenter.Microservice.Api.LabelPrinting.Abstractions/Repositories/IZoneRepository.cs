using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

/// <summary>Acceso al catalogo de zonas logisticas.</summary>
public interface IZoneRepository
{
    /// <summary>Lista las zonas activas ordenadas por codigo.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Zonas disponibles para operar.</returns>
    Task<IReadOnlyCollection<Zone>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Resuelve una zona por su codigo operativo.</summary>
    /// <param name="code">Codigo de zona, por ejemplo ZONA-PICKING-A.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La zona activa, o null si no existe.</returns>
    Task<Zone?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
