namespace VideoHub.Api.Domain.Jobs;

public static class JobStatuses
{
    public const string Queued = "Queued";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string PartiallyCompleted = "PartiallyCompleted";
    public const string Failed = "Failed";
}
