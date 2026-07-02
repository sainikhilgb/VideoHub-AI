namespace VideoHub.Api.Application.BackgroundJobs;

public interface IBackgroundJobService
{
    string QueueHelloWorld();

    string ScheduleHelloWorld(TimeSpan delay);

    string RegisterRecurringHelloWorld();

    string QueueContinuationHelloWorld();

    string QueueMediaProcessingJob(Guid jobId, Guid mediaFileId);

    Task ExecuteHelloWorldAsync(string jobType, CancellationToken cancellationToken = default);

    Task ExecuteMediaProcessingAsync(Guid jobId, Guid mediaFileId, CancellationToken cancellationToken = default);
}
