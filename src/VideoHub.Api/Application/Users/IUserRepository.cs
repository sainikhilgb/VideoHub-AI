using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Infrastructure.Abstractions;

namespace VideoHub.Api.Application.Users;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
