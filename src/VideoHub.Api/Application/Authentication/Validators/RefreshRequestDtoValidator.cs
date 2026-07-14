using FluentValidation;
using VideoHub.Api.Application.Authentication.DTOs;

namespace VideoHub.Api.Application.Authentication.Validators;

public sealed class RefreshRequestDtoValidator : AbstractValidator<RefreshRequestDto>
{
    public RefreshRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
