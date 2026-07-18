using System;
using System.Text;
using VideoHub.Api.Application.DTOs;

namespace VideoHub.Api.Application.Captions;

public interface ICaptionGeneratorService
{
    string GenerateSrt(TranscriptContentDto transcript);
    string GenerateVtt(TranscriptContentDto transcript);
}

public class CaptionGeneratorService : ICaptionGeneratorService
{
    public string GenerateSrt(TranscriptContentDto transcript)
    {
        var sb = new StringBuilder();
        int counter = 1;

        foreach (var segment in transcript.Segments)
        {
            sb.AppendLine(counter.ToString());
            sb.AppendLine($"{FormatTimeSrt(segment.Start)} --> {FormatTimeSrt(segment.End)}");
            sb.AppendLine(segment.Text.Trim());
            sb.AppendLine();
            counter++;
        }

        return sb.ToString();
    }

    public string GenerateVtt(TranscriptContentDto transcript)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WEBVTT");
        sb.AppendLine();

        foreach (var segment in transcript.Segments)
        {
            sb.AppendLine($"{FormatTimeVtt(segment.Start)} --> {FormatTimeVtt(segment.End)}");
            sb.AppendLine(segment.Text.Trim());
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string FormatTimeSrt(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00},{ts.Milliseconds:000}";
    }

    private string FormatTimeVtt(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
    }
}
