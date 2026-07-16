namespace VideoHub.Api.Application.BackgroundJobs;

public interface IBackgroundJobService
{
    string QueueHelloWorld();

    string ScheduleHelloWorld(TimeSpan delay);

    string RegisterRecurringHelloWorld();

    string QueueContinuationHelloWorld();

    string QueueMediaProcessingJob(Guid jobId, Guid mediaFileId, string? correlationId = null);

    Task ExecuteHelloWorldAsync(string jobType, CancellationToken cancellationToken = default);

    Task ExecuteMediaProcessingAsync(Guid jobId, Guid mediaFileId, string? correlationId = null, CancellationToken cancellationToken = default);

    Task ExecuteMediaProcessingAsync(Guid jobId, Guid mediaFileId, CancellationToken cancellationToken);

    string QueueCombinedMediaJob(Guid combinedMediaId);

    Task ExecuteCombinedMediaJobAsync(Guid combinedMediaId, CancellationToken cancellationToken = default);
}
