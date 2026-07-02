namespace VideoHub.Api.Application.BackgroundJobs;

public interface IBackgroundJobService
{
    string QueueHelloWorld();

    string ScheduleHelloWorld(TimeSpan delay);

    string RegisterRecurringHelloWorld();

    string QueueContinuationHelloWorld();

    Task ExecuteHelloWorldAsync(string jobType, CancellationToken cancellationToken = default);
}
