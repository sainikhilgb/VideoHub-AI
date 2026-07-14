namespace VideoHub.Api.Application.Authentication.DTOs;

public sealed record RegisterRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword);
