using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoHub.Api.Application.Authentication.DTOs;
using VideoHub.Api.Application.Exceptions;
using VideoHub.Api.Application.Users;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Authentication;

namespace VideoHub.Api.Application.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IJwtService jwtService;
    private readonly IUnitOfWork unitOfWork;
    private readonly IOptions<JwtOptions> jwtOptions;
    private readonly ILogger<AuthenticationService> logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthenticationService> logger)
    {
        this.userRepository = userRepository;
        this.refreshTokenRepository = refreshTokenRepository;
        this.passwordHasher = passwordHasher;
        this.jwtService = jwtService;
        this.unitOfWork = unitOfWork;
        this.jwtOptions = jwtOptions;
        this.logger = logger;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Registration Started: Email={Email}", request.Email);

        var existingUser = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            logger.LogWarning("Registration Failed: Email={Email} is already in use", request.Email);
            throw new ConflictException("Email is already in use.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            DisplayName = $"{request.FirstName} {request.LastName}",
            Role = "User",
            PasswordHash = passwordHasher.HashPassword(request.Password),
            IsActive = true,
            EmailVerified = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Registration Success: UserId={UserId} Email={Email}", user.Id, user.Email);

        return new RegisterResponseDto(user.Id, user.Email, user.FirstName, user.LastName);
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Login Started: Email={Email}", request.Email);

        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            logger.LogWarning("Login Failed (Invalid Credentials): Email={Email}", request.Email);
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login Failed (Inactive User): Email={Email}", request.Email);
            throw new AuthenticationException("User account is inactive.");
        }

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshTokenString = jwtService.GenerateRefreshToken();

        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        userRepository.Update(user);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpiryDays),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            Device = ParseDevice(userAgent)
        };

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Login Success: UserId={UserId} Email={Email}", user.Id, user.Email);

        return new LoginResponseDto(accessToken, refreshTokenString, jwtOptions.Value.ExpiryMinutes * 60);
    }

    public async Task<RefreshResponseDto> RefreshAsync(
        RefreshRequestDto request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Token Refresh Started");

        var oldRefreshToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (oldRefreshToken is null)
        {
            logger.LogWarning("Token Refresh Failed (Invalid Token)");
            throw new InvalidRefreshTokenException("Invalid refresh token.");
        }

        if (oldRefreshToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Token Refresh Failed (Expired Token): TokenId={TokenId} UserId={UserId}", oldRefreshToken.Id, oldRefreshToken.UserId);
            throw new TokenExpiredException("Refresh token has expired.");
        }

        if (oldRefreshToken.RevokedAt is not null)
        {
            logger.LogWarning("Token Refresh Failed (Revoked Token Reused): TokenId={TokenId} UserId={UserId} - Potential Replay Attack", oldRefreshToken.Id, oldRefreshToken.UserId);
            throw new InvalidRefreshTokenException("Refresh token has been revoked.");
        }

        // Revoke the old token
        oldRefreshToken.RevokedAt = DateTimeOffset.UtcNow;
        oldRefreshToken.RevokedByIp = ipAddress;
        refreshTokenRepository.Update(oldRefreshToken);

        var user = oldRefreshToken.User;
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Token Refresh Failed (Inactive User): UserId={UserId}", oldRefreshToken.UserId);
            throw new AuthenticationException("User account is inactive or not found.");
        }

        var newAccessToken = jwtService.GenerateAccessToken(user);
        var newRefreshTokenString = jwtService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenString,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpiryDays),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            Device = oldRefreshToken.Device
        };

        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Token Refresh Success: UserId={UserId}", user.Id);

        return new RefreshResponseDto(newAccessToken, newRefreshTokenString, jwtOptions.Value.ExpiryMinutes * 60);
    }

    public async Task LogoutAsync(string refreshTokenString, string ipAddress, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Logout Started");

        var refreshToken = await refreshTokenRepository.GetByTokenAsync(refreshTokenString, cancellationToken);
        if (refreshToken is not null && refreshToken.RevokedAt is null)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshTokenRepository.Update(refreshToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Logout Success: UserId={UserId} TokenId={TokenId}", refreshToken.UserId, refreshToken.Id);
        }
        else
        {
            logger.LogInformation("Logout: Token not found or already revoked");
        }
    }

    private static string ParseDevice(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        if (userAgent.Contains("Mobi", StringComparison.OrdinalIgnoreCase))
            return "Mobile";

        if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            return "Tablet";

        return "Desktop";
    }
}
