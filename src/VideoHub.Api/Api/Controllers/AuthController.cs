using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VideoHub.Api.Application.Authentication;
using VideoHub.Api.Application.Authentication.DTOs;
using VideoHub.Api.Application.CurrentUser;
using VideoHub.Api.Application.Users;
using VideoHub.Api.Infrastructure.Authentication;

namespace VideoHub.Api.Api.Controllers;

public sealed record SecureTokenResponseDto(string AccessToken, int ExpiresIn);

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService authenticationService;
    private readonly ICurrentUserService currentUserService;
    private readonly IUserRepository userRepository;
    private readonly JwtOptions jwtOptions;

    public AuthController(
        IAuthenticationService authenticationService,
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IOptions<JwtOptions> jwtOptions)
    {
        this.authenticationService = authenticationService;
        this.currentUserService = currentUserService;
        this.userRepository = userRepository;
        this.jwtOptions = jwtOptions.Value;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var response = await authenticationService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(SecureTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();
        var response = await authenticationService.LoginAsync(request, ipAddress, userAgent, cancellationToken);
        
        SetRefreshTokenCookie(response.RefreshToken);
        
        return Ok(new SecureTokenResponseDto(response.AccessToken, response.ExpiresIn));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(SecureTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] RefreshRequestDto? request, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"] ?? request?.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();
        var response = await authenticationService.RefreshAsync(new RefreshRequestDto(refreshToken), ipAddress, userAgent, cancellationToken);
        
        SetRefreshTokenCookie(response.RefreshToken);
        
        return Ok(new SecureTokenResponseDto(response.AccessToken, response.ExpiresIn));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] RefreshRequestDto? request, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"] ?? request?.RefreshToken;
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var ipAddress = GetIpAddress();
            await authenticationService.LogoutAsync(refreshToken, ipAddress, cancellationToken);
        }
        
        // Remove cookie on logout
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });
        
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        var user = await userRepository.GetByIdAsync(currentUserService.UserId, cancellationToken);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        var response = new CurrentUserResponseDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.DisplayName ?? string.Empty,
            user.Role,
            user.IsActive,
            user.CreatedAt
        );

        return Ok(response);
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.RefreshTokenExpiryDays)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
