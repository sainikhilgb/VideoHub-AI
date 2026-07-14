namespace VideoHub.Api.Application.Authentication.DTOs;

public sealed record LoginRequestDto(
    string Email,
    string Password);
