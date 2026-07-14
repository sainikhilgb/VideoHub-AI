using Microsoft.AspNetCore.Identity;
using VideoHub.Api.Application.Authentication;
using VideoHub.Api.Domain.Entities;

namespace VideoHub.Api.Infrastructure.Authentication;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> hasher = new();

    public string HashPassword(string password)
    {
        return hasher.HashPassword(new User(), password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = hasher.VerifyHashedPassword(new User(), hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}
