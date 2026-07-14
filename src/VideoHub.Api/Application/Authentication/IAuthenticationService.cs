using VideoHub.Api.Application.Authentication.DTOs;

namespace VideoHub.Api.Application.Authentication;

public interface IAuthenticationService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    Task<RefreshResponseDto> RefreshAsync(RefreshRequestDto request, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default);
}
