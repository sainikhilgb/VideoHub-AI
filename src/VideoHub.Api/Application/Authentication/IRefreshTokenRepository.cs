using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Infrastructure.Abstractions;

namespace VideoHub.Api.Application.Authentication;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
}
