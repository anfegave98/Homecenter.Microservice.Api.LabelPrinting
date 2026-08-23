using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Repositories;

/// <summary>Componente UserRepository del submodulo de impresion.</summary>
public sealed class UserRepository : IUserRepository
{
    private readonly LabelPrintingDbContext _context;

    /// <summary>Crea una instancia con sus dependencias.</summary>
    public UserRepository(LabelPrintingDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserName == userName && x.State, cancellationToken);

    /// <inheritdoc />
    public async Task UpdateLastLoginAsync(int idUser, DateTimeOffset loginDate, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == idUser, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.LastLoginDate = loginDate;
        user.DateModified = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
