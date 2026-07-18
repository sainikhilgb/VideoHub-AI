using System.Collections.Generic;

namespace VideoHub.Api.Application.DTOs;

public class TranscriptWordDto
{
    public string Text { get; set; } = string.Empty;
    public double Start { get; set; }
    public double End { get; set; }
    public double? Confidence { get; set; }
}

public class TranscriptSegmentDto
{
    public double Start { get; set; }
    public double End { get; set; }
    public string Text { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public List<TranscriptWordDto>? Words { get; set; }
}

public class TranscriptContentDto
{
    public string DetectedLanguage { get; set; } = string.Empty;
    public List<TranscriptSegmentDto> Segments { get; set; } = new();
}

public class TranscriptUpdateRequestDto
{
    public TranscriptContentDto Content { get; set; } = new();
}
