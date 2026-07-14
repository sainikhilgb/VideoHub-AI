using System.Security.Claims;
using VideoHub.Api.Domain.Entities;

namespace VideoHub.Api.Application.Authentication;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
