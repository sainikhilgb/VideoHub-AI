using Microsoft.AspNetCore.Mvc;

namespace VideoHub.Api.Application.BackgroundJobs;

[ApiController]
[Route("api/jobs")]
public sealed class BackgroundJobsController : ControllerBase
{
    private readonly IBackgroundJobService backgroundJobService;

    public BackgroundJobsController(IBackgroundJobService backgroundJobService)
    {
        this.backgroundJobService = backgroundJobService;
    }

    [HttpPost("hello-world")]
    public IActionResult QueueHelloWorld()
    {
        var jobId = backgroundJobService.QueueHelloWorld();
        return Accepted(new { jobId, jobType = "Fire-and-Forget" });
    }

    [HttpPost("hello-world/delayed/{delaySeconds:int}")]
    public IActionResult ScheduleHelloWorld([FromRoute] int delaySeconds)
    {
        var jobId = backgroundJobService.ScheduleHelloWorld(TimeSpan.FromSeconds(delaySeconds));
        return Accepted(new { jobId, jobType = "Delayed", delaySeconds });
    }

    [HttpPost("hello-world/recurring")]
    public IActionResult RegisterRecurringHelloWorld()
    {
        var recurringJobId = backgroundJobService.RegisterRecurringHelloWorld();
        return Accepted(new { jobId = recurringJobId, jobType = "Recurring" });
    }

    [HttpPost("hello-world/continuation")]
    public IActionResult QueueContinuationHelloWorld()
    {
        var jobId = backgroundJobService.QueueContinuationHelloWorld();
        return Accepted(new { jobId, jobType = "Continuation" });
    }
}
