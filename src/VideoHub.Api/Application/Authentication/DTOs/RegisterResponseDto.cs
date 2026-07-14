namespace VideoHub.Api.Application.Authentication.DTOs;

public sealed record RegisterResponseDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName);
