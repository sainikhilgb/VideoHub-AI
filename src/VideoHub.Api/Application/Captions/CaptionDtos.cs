namespace VideoHub.Api.Application.Captions;



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
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
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
    public string TranscriptBlobUrl { get; set; } = string.Empty;
    public IReadOnlyList<LanguageResult> LanguageResults { get; set; } = [];
}

public sealed class LanguageResult
{
    public string LanguageCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public sealed class ProjectCaptionResponseDto
{
    public Guid Id { get; set; }
    public Guid? JobId { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BlobUrl { get; set; }
}
