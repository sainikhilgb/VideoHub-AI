using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Application.Users;
using VideoHub.Api.Domain.Entities;

namespace VideoHub.Api.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : EfRepository<User>, IUserRepository
{
    private readonly AppDbContext dbContext;

    public UserRepository(AppDbContext dbContext)
        : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }
}
