using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Application.Authentication;
using VideoHub.Api.Domain.Entities;

namespace VideoHub.Api.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : EfRepository<RefreshToken>, IRefreshTokenRepository
{
    private readonly AppDbContext dbContext;

    public RefreshTokenRepository(AppDbContext dbContext)
        : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var hashedToken = HashToken(token);
        return await dbContext.Set<RefreshToken>()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken, cancellationToken);
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
