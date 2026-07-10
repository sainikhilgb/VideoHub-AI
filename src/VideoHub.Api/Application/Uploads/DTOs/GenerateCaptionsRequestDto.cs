namespace VideoHub.Api.Application.DTOs;

public sealed class GenerateCaptionsRequestDto
{
    public List<string> TargetLanguages { get; set; } = [];
}
