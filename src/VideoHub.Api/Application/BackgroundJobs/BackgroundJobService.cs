using Hangfire;
using Microsoft.Extensions.Logging;

namespace VideoHub.Api.Application.BackgroundJobs;

public sealed class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient backgroundJobClient;
    private readonly IRecurringJobManager recurringJobManager;
    private readonly ILogger<BackgroundJobService> logger;

    public BackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        ILogger<BackgroundJobService> logger)
    {
        this.backgroundJobClient = backgroundJobClient;
        this.recurringJobManager = recurringJobManager;
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
}
