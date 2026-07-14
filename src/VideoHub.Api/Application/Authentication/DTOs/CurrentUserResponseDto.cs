namespace VideoHub.Api.Application.Authentication.DTOs;

public sealed record CurrentUserResponseDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);
