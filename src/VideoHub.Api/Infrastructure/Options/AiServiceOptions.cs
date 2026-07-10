namespace VideoHub.Api.Infrastructure.Options;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    /// <summary>Base URL of the Python AI service (e.g. http://localhost:8000).</summary>
    public string BaseUrl { get; set; } = "http://localhost:8000";
}
