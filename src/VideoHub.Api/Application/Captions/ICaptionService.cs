namespace VideoHub.Api.Application.Captions;

public interface ICaptionService
{
    /// <summary>
    /// Pre-creates CaptionFile records and Supabase storage folders for each requested language,
    /// then dispatches an asynchronous processing request to the Python AI service.
    /// </summary>
    Task DispatchCaptionGenerationAsync(
        Guid jobId,
        Guid projectId,
        Guid mediaFileId,
        Guid userId,
        string storagePath,
        string mediaType,
        string bucket,
        string originalLanguage,
        IReadOnlyList<string> targetLanguages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an individual CaptionFile status from a Python per-language callback.
    /// </summary>
    Task UpdateCaptionFileStatusAsync(
        Guid captionFileId,
        string status,
        string? blobUrl,
        string? errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates all CaptionFile statuses for a job and sets the parent Job's terminal status.
    /// </summary>
    Task FinalizeJobAsync(
        Guid jobId,
        string detectedLanguage,
        IReadOnlyList<TranscriptSegmentDto> segments,
        CancellationToken cancellationToken = default);
}
