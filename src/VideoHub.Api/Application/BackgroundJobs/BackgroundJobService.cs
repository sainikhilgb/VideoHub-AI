using Hangfire;
using Microsoft.Extensions.Logging;
using VideoHub.Api.Application.Captions;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Domain.Jobs;
using VideoHub.Api.Domain.Media;
using VideoHub.Api.Infrastructure.Abstractions;

namespace VideoHub.Api.Application.BackgroundJobs;

public sealed class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient backgroundJobClient;
    private readonly IRecurringJobManager recurringJobManager;
    private readonly IRepository<Job> jobRepository;
    private readonly IRepository<MediaFile> mediaFileRepository;
    private readonly IRepository<Project> projectRepository;
    private readonly ICaptionService captionService;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<BackgroundJobService> logger;

    public BackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        IRepository<Job> jobRepository,
        IRepository<MediaFile> mediaFileRepository,
        IRepository<Project> projectRepository,
        ICaptionService captionService,
        IUnitOfWork unitOfWork,
        ILogger<BackgroundJobService> logger)
    {
        this.backgroundJobClient = backgroundJobClient;
        this.recurringJobManager = recurringJobManager;
        this.jobRepository = jobRepository;
        this.mediaFileRepository = mediaFileRepository;
        this.projectRepository = projectRepository;
        this.captionService = captionService;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public string QueueContinuationHelloWorld()
    {
        logger.LogInformation("Job Queued: Continuation job chain");

        var parentJobId = backgroundJobClient.Enqueue(() => ExecuteHelloWorldAsync("Fire-and-Forget", CancellationToken.None));
        return backgroundJobClient.ContinueJobWith(parentJobId, () => ExecuteHelloWorldAsync("Continuation", CancellationToken.None));
    }

    public string QueueHelloWorld()
    {
        logger.LogInformation("Job Queued: Fire-and-forget job");
        return backgroundJobClient.Enqueue(() => ExecuteHelloWorldAsync("Fire-and-Forget", CancellationToken.None));
    }

    public string QueueMediaProcessingJob(Guid jobId, Guid mediaFileId, string? correlationId = null)
    {
        logger.LogInformation("Job Queued: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
        return backgroundJobClient.Enqueue(() => ExecuteMediaProcessingAsync(jobId, mediaFileId, correlationId, CancellationToken.None));
    }

    public string RegisterRecurringHelloWorld()
    {
        const string recurringJobId = "hello-world-recurring";
        logger.LogInformation("Job Queued: Recurring job {RecurringJobId}", recurringJobId);

        recurringJobManager.AddOrUpdate(
            recurringJobId,
            () => ExecuteHelloWorldAsync("Recurring", CancellationToken.None),
            Cron.Minutely);

        return recurringJobId;
    }

    public string ScheduleHelloWorld(TimeSpan delay)
    {
        logger.LogInformation("Job Queued: Delayed job with delay {Delay}", delay);
        return backgroundJobClient.Schedule(() => ExecuteHelloWorldAsync("Delayed", CancellationToken.None), delay);
    }

    public async Task ExecuteHelloWorldAsync(string jobType, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Job Started: {JobType}", jobType);

        try
        {
            await Task.Delay(250, cancellationToken);
            logger.LogInformation("Hello from Hangfire. JobType={JobType}", jobType);
            logger.LogInformation("Job Completed: {JobType}", jobType);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Job Failed: {JobType}", jobType);
            throw;
        }
    }

    public async Task ExecuteMediaProcessingAsync(Guid jobId, Guid mediaFileId, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        using (Serilog.Context.LogContext.PushProperty("JobId", jobId.ToString()))
        using (Serilog.Context.LogContext.PushProperty("MediaId", mediaFileId.ToString()))
        {
            logger.LogInformation("Job Started: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);

            var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
            if (job is null)
            {
                logger.LogWarning("Job Failed: Media processing references missing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
                return;
            }

            var mediaFile = await mediaFileRepository.GetByIdAsync(mediaFileId, cancellationToken);
            if (mediaFile is null)
            {
                logger.LogWarning("Job Failed: Media processing references missing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
                job.Status = JobStatuses.Failed;
                job.StatusMessage = $"Media file not found: {mediaFileId}";
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var project = await projectRepository.GetByIdAsync(job.ProjectId, cancellationToken);
            if (project is null)
            {
                logger.LogWarning("Job Failed: Project not found ProjectId={ProjectId} JobId={JobId}", job.ProjectId, jobId);
                job.Status = JobStatuses.Failed;
                job.StatusMessage = $"Project not found: {job.ProjectId}";
                mediaFile.Status = MediaFileStatuses.Failed;
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            using (Serilog.Context.LogContext.PushProperty("ProjectId", project.Id.ToString()))
            {
                try
                {
                    mediaFile.Status = MediaFileStatuses.Processing;
                    job.StartedAt = DateTimeOffset.UtcNow;
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    // Resolve target languages from the job configuration, falling back to original language if none specified.
                    var targetLanguages = !string.IsNullOrWhiteSpace(job.TargetLanguages)
                        ? job.TargetLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(lang => lang.Trim()).ToList()
                        : new List<string> { project.OriginalLanguage };

                    await captionService.DispatchCaptionGenerationAsync(
                        jobId,
                        project.Id,
                        mediaFile.Id,
                        project.UserId,
                        mediaFile.StoragePath,
                        mediaFile.Type,
                        mediaFile.Bucket,
                        project.OriginalLanguage,
                        targetLanguages,
                        correlationId,
                        cancellationToken);

                    logger.LogInformation("Job Dispatched: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
                }
                catch (Exception exception)
                {
                    job.Status = JobStatuses.Failed;
                    job.Attempts += 1;
                    job.StatusMessage = exception.Message != null && exception.Message.Length > 500
                        ? exception.Message.Substring(0, 500)
                        : exception.Message;
                    mediaFile.Status = MediaFileStatuses.Failed;
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    logger.LogError(exception, "Job Failed: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
                    throw;
                }
            }
        }
    }
}
