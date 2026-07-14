namespace VideoHub.Api.Application.Authentication.DTOs;

public sealed record RefreshResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
