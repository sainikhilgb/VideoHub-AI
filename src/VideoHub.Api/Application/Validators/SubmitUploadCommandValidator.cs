using FluentValidation;
using VideoHub.Api.Application.Commands;

namespace VideoHub.Api.Application.Validators;

public sealed class SubmitUploadCommandValidator : AbstractValidator<SubmitUploadCommand>
{
    public SubmitUploadCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.OriginalFileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.ContentType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.FileSizeBytes)
            .GreaterThan(0);

        RuleFor(command => command.Extension)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(command => command.Content)
            .NotNull();
    }
}
