using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

public interface IZoneRepository
{
    Task<IReadOnlyCollection<Zone>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Zone?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
