using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

public interface IUserRepository
{
    /// <summary>Obtiene el usuario activo con sus roles cargados, o null si no existe.</summary>
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    Task UpdateLastLoginAsync(int idUser, DateTimeOffset loginDate, CancellationToken cancellationToken = default);
}
