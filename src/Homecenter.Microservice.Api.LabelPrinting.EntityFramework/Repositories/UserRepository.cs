using Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly LabelPrintingDbContext _context;

    public UserRepository(LabelPrintingDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserName == userName && x.State, cancellationToken);

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
