using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VideoHub.Api.Application.Authentication;
using VideoHub.Api.Application.Authentication.DTOs;
using VideoHub.Api.Application.CurrentUser;
using VideoHub.Api.Application.Users;

namespace VideoHub.Api.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService authenticationService;
    private readonly ICurrentUserService currentUserService;
    private readonly IUserRepository userRepository;

    public AuthController(
        IAuthenticationService authenticationService,
        ICurrentUserService currentUserService,
        IUserRepository userRepository)
    {
        this.authenticationService = authenticationService;
        this.currentUserService = currentUserService;
        this.userRepository = userRepository;
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
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();
        var response = await authenticationService.LoginAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();
        var response = await authenticationService.RefreshAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        await authenticationService.LogoutAsync(request.RefreshToken, ipAddress, cancellationToken);
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

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
