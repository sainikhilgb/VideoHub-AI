using FluentValidation;
using VideoHub.Api.Application.DTOs;

namespace VideoHub.Api.Application.Validators;

public sealed class UploadRequestDtoValidator : AbstractValidator<UploadRequestDto>
{
    public UploadRequestDtoValidator()
    {
        RuleFor(request => request.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(request => request.ContentType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.FileSizeBytes)
            .GreaterThan(0);
    }
}
