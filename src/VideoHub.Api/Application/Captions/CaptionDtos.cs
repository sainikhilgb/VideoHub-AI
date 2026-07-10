namespace VideoHub.Api.Application.Captions;

public sealed class TranscriptSegmentDto
{
    public double Start { get; set; }
    public double End { get; set; }
    public string Text { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public IReadOnlyList<WordDto> Words { get; set; } = [];
}

public sealed class WordDto
{
    public string Text { get; set; } = string.Empty;
    public double Start { get; set; }
    public double End { get; set; }
    public double? Confidence { get; set; }
}

/// <summary>Sent from .NET → Python to initiate processing.</summary>
public sealed class AiProcessRequest
{
    public Guid JobId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid MediaId { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string OriginalLanguage { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public IReadOnlyList<LanguageTarget> Languages { get; set; } = [];
}

public sealed class LanguageTarget
{
    public string LanguageCode { get; set; } = string.Empty;
    public Dictionary<string, Guid> CaptionFileIds { get; set; } = new(); // format → CaptionFile.Id
    public string FolderPath { get; set; } = string.Empty;
}

/// <summary>Received from Python → .NET as per-language status callback.</summary>
public sealed class CaptionStatusCallbackDto
{
    public string Status { get; set; } = string.Empty;
    public string? BlobUrl { get; set; }
    public string? Error { get; set; }
}

/// <summary>Received from Python → .NET as the final summary callback.</summary>
public sealed class AiProcessCallbackDto
{
    public string DetectedLanguage { get; set; } = string.Empty;
    public IReadOnlyList<TranscriptSegmentDto> Segments { get; set; } = [];
    public IReadOnlyList<LanguageResult> LanguageResults { get; set; } = [];
}

public sealed class LanguageResult
{
    public string LanguageCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
}
