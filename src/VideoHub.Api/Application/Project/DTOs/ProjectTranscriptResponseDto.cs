using System;

namespace VideoHub.Api.Application.DTOs;

public sealed record ProjectTranscriptResponseDto(
    Guid Id,
    string Language,
    string Status,
    string? BlobUrl,
    int Version);
