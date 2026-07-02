using FluentValidation;
using VideoHub.Api.Application.Commands;

namespace VideoHub.Api.Application.Validators;

public sealed class SubmitUploadCommandValidator : AbstractValidator<SubmitUploadCommand>
{
    public SubmitUploadCommandValidator()
    {
        RuleFor(command => command.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.ContentType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.FileSizeBytes)
            .GreaterThan(0);
    }
}
