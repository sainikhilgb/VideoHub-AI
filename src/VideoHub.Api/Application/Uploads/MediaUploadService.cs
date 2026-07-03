using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoHub.Api.Application.BackgroundJobs;
using VideoHub.Api.Application.Commands;
using VideoHub.Api.Application.DTOs;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Domain.Jobs;
using VideoHub.Api.Domain.Media;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Options;

namespace VideoHub.Api.Application.Uploads;

public sealed class MediaUploadService : IMediaUploadService
{
    private readonly IRepository<Project> projectRepository;
    private readonly IRepository<MediaFile> mediaFileRepository;
    private readonly IRepository<Job> jobRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IBlobStorage blobStorage;
    private readonly IMediaStoragePathBuilder mediaStoragePathBuilder;
    private readonly IBackgroundJobService backgroundJobService;
    private readonly IValidator<SubmitUploadCommand> commandValidator;
    private readonly IOptions<BlobStorageOptions> blobStorageOptions;
    private readonly ILogger<MediaUploadService> logger;

    public MediaUploadService(
        IRepository<Project> projectRepository,
        IRepository<MediaFile> mediaFileRepository,
        IRepository<Job> jobRepository,
        IUnitOfWork unitOfWork,
        IBlobStorage blobStorage,
        IMediaStoragePathBuilder mediaStoragePathBuilder,
        IBackgroundJobService backgroundJobService,
        IValidator<SubmitUploadCommand> commandValidator,
        IOptions<BlobStorageOptions> blobStorageOptions,
        ILogger<MediaUploadService> logger)
    {
        this.projectRepository = projectRepository;
        this.mediaFileRepository = mediaFileRepository;
        this.jobRepository = jobRepository;
        this.unitOfWork = unitOfWork;
        this.blobStorage = blobStorage;
        this.mediaStoragePathBuilder = mediaStoragePathBuilder;
        this.backgroundJobService = backgroundJobService;
        this.commandValidator = commandValidator;
        this.blobStorageOptions = blobStorageOptions;
        this.logger = logger;
    }

    public async Task<UploadMediaResponseDto> UploadAsync(SubmitUploadCommand command, CancellationToken cancellationToken = default)
    {
        await commandValidator.ValidateAndThrowAsync(command, cancellationToken);

        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken);
        if (project is null)
        {
            throw new KeyNotFoundException($"Project '{command.ProjectId}' was not found.");
        }

        var mediaType = ResolveMediaType(command.Extension);
        var storedFileName = $"{Guid.NewGuid():N}{command.Extension.ToLowerInvariant()}";
        var storagePath = mediaStoragePathBuilder.Build(project.UserId, project.Id, mediaType, storedFileName);
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = project.UserId,
            Type = mediaType,
            OriginalFileName = command.OriginalFileName,
            StoredFileName = storedFileName,
            Bucket = blobStorageOptions.Value.BucketName ?? "media",
            StoragePath = storagePath,
            MimeType = command.ContentType,
            Extension = command.Extension.ToLowerInvariant(),
            FileSize = command.FileSizeBytes,
            Status = MediaFileStatuses.Uploading,
            UploadedAt = DateTimeOffset.UtcNow
        };

        logger.LogInformation("Upload Started: ProjectId={ProjectId} FileName={FileName}", project.Id, command.OriginalFileName);

        try
        {
            command.Content.Position = 0;
            await blobStorage.UploadAsync(command.Content, storagePath, command.ContentType, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Storage Upload Failed: ProjectId={ProjectId} FileName={FileName}", project.Id, command.OriginalFileName);
            throw;
        }

        mediaFile.Status = MediaFileStatuses.Uploaded;
        await mediaFileRepository.AddAsync(mediaFile, cancellationToken);

        var processingJob = new Job
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Type = JobTypes.MediaProcessing,
            Status = JobStatuses.Queued
        };

        await jobRepository.AddAsync(processingJob, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var processingJobId = backgroundJobService.QueueMediaProcessingJob(processingJob.Id, mediaFile.Id);

        logger.LogInformation(
            "Upload Completed: ProjectId={ProjectId} MediaId={MediaId} StoragePath={StoragePath}",
            project.Id,
            mediaFile.Id,
            mediaFile.StoragePath);

        return new UploadMediaResponseDto(
            mediaFile.Id,
            project.Id,
            mediaFile.Status,
            mediaFile.StoragePath,
            processingJobId);
    }

    private static string ResolveMediaType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm" => MediaFileTypes.Video,
            ".mp3" or ".wav" or ".m4a" or ".aac" or ".flac" => MediaFileTypes.Audio,
            _ => MediaFileTypes.Document
        };
    }
}
