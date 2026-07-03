using FluentValidation;
using Microsoft.AspNetCore.Http;
using VideoHub.Api.Application.DTOs;
using VideoHub.Api.Infrastructure.Options;

namespace VideoHub.Api.Application.Validators;

public sealed class UploadMediaRequestDtoValidator : AbstractValidator<UploadMediaRequestDto>
{
    private static readonly HashSet<string> VideoExtensions =
    [
        ".mp4", ".mov", ".avi", ".mkv", ".webm"
    ];

    private static readonly HashSet<string> AudioExtensions =
    [
        ".mp3", ".wav", ".m4a", ".aac", ".flac"
    ];

    private static readonly HashSet<string> VideoMimeTypes =
    [
        "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm"
    ];

    private static readonly HashSet<string> AudioMimeTypes =
    [
        "audio/mpeg", "audio/wav", "audio/x-wav", "audio/mp4", "audio/aac", "audio/flac"
    ];

    public UploadMediaRequestDtoValidator()
    {
        RuleFor(request => request.File)
            .NotNull()
            .WithMessage("A file is required.");

        When(request => request.File is not null, () =>
        {
            RuleFor(request => request.File!.FileName)
                .Must(HaveSupportedExtension)
                .WithMessage("Unsupported file extension.");

            RuleFor(request => request.File!.ContentType)
                .Must(HaveSupportedMimeType)
                .WithMessage("Unsupported MIME type.");

            RuleFor(request => request.File!)
                .Must(BeWithinAllowedSize)
                .WithMessage("File exceeds the allowed size for its type.");
        });
    }

    private static bool BeWithinAllowedSize(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (VideoExtensions.Contains(extension))
        {
            return file.Length <= MediaUploadOptions.VideoMaxBytes;
        }

        if (AudioExtensions.Contains(extension))
        {
            return file.Length <= MediaUploadOptions.AudioMaxBytes;
        }

        return false;
    }

    private static bool HaveSupportedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return VideoExtensions.Contains(extension) || AudioExtensions.Contains(extension);
    }

    private static bool HaveSupportedMimeType(string contentType) =>
        VideoMimeTypes.Contains(contentType.ToLowerInvariant()) ||
        AudioMimeTypes.Contains(contentType.ToLowerInvariant());
}
