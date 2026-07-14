namespace VideoHub.Api.Application.Authentication.DTOs;

public sealed record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
