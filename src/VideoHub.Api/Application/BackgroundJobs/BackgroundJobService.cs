using Hangfire;
using Microsoft.Extensions.Logging;
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
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<BackgroundJobService> logger;

    public BackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        IRepository<Job> jobRepository,
        IRepository<MediaFile> mediaFileRepository,
        IUnitOfWork unitOfWork,
        ILogger<BackgroundJobService> logger)
    {
        this.backgroundJobClient = backgroundJobClient;
        this.recurringJobManager = recurringJobManager;
        this.jobRepository = jobRepository;
        this.mediaFileRepository = mediaFileRepository;
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

    public string QueueMediaProcessingJob(Guid jobId, Guid mediaFileId)
    {
        logger.LogInformation("Job Queued: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
        return backgroundJobClient.Enqueue(() => ExecuteMediaProcessingAsync(jobId, mediaFileId, CancellationToken.None));
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

    public async Task ExecuteMediaProcessingAsync(Guid jobId, Guid mediaFileId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Job Started: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);

        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        var mediaFile = await mediaFileRepository.GetByIdAsync(mediaFileId, cancellationToken);

        if (job is null || mediaFile is null)
        {
            logger.LogWarning("Job Failed: Media processing references missing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
            return;
        }

        try
        {
            job.Status = JobStatuses.Processing;
            job.StartedAt = DateTimeOffset.UtcNow;
            mediaFile.Status = MediaFileStatuses.Processing;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Processing placeholder executed for MediaId={MediaId}", mediaFileId);

            job.Status = JobStatuses.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            mediaFile.Status = MediaFileStatuses.Completed;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Job Completed: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
        }
        catch (Exception exception)
        {
            job.Status = JobStatuses.Failed;
            job.Attempts += 1;
            mediaFile.Status = MediaFileStatuses.Failed;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogError(exception, "Job Failed: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
            throw;
        }
    }
}
