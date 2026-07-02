using Hangfire.Common;
using Hangfire.States;
using Hangfire.Server;
using Serilog;

namespace VideoHub.Api.Infrastructure.BackgroundJobs;

public sealed class HangfireJobExecutionLoggingFilter : JobFilterAttribute, IServerFilter, IElectStateFilter
{
    public void OnPerforming(PerformingContext context)
    {
        Log.Information("Job Started: {JobId} {JobType}", context.BackgroundJob.Id, context.BackgroundJob.Job?.Type?.Name);
    }

    public void OnPerformed(PerformedContext context)
    {
        if (context.Exception is null)
        {
            Log.Information("Job Completed: {JobId}", context.BackgroundJob.Id);
            return;
        }

        Log.Error(context.Exception, "Job Failed: {JobId}", context.BackgroundJob.Id);
    }

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is ScheduledState scheduledState)
        {
            var retryCount = context.GetJobParameter<int>("RetryCount");
            if (retryCount <= 0)
            {
                return;
            }

            Log.Warning(
                "Retry Attempt Scheduled: {JobId} RetryCount={RetryCount} EnqueueAt={EnqueueAt}",
                context.BackgroundJob.Id,
                retryCount,
                scheduledState.EnqueueAt);
        }
    }
}
