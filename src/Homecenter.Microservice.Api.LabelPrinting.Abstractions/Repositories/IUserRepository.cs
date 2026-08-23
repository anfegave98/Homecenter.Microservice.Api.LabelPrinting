using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

/// <summary>Acceso a los usuarios operativos del submodulo.</summary>
public interface IUserRepository
{
    /// <summary>Obtiene el usuario activo con sus roles cargados, o null si no existe.</summary>
    /// <param name="userName">Nombre de usuario a buscar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El usuario con sus roles, o null.</returns>
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>Registra la fecha del ultimo inicio de sesion.</summary>
    /// <param name="idUser">Identificador del usuario.</param>
    /// <param name="loginDate">Momento del inicio de sesion, en UTC.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task UpdateLastLoginAsync(int idUser, DateTimeOffset loginDate, CancellationToken cancellationToken = default);
}
