using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VideoHub.Api.Application.CurrentUser;

namespace VideoHub.Api.Infrastructure.Authentication;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID claim is missing or invalid.");
            }
            
            return userId;
        }
    }

    public string Email =>
        httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles =>
        httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();
}
