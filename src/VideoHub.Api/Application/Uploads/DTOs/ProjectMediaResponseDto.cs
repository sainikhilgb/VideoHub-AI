using System;

namespace VideoHub.Api.Application.DTOs;

public sealed record ProjectMediaResponseDto(
    Guid Id,
    Guid ProjectId,
    string FileName,
    string ContentType,
    long FileSize,
    string Status,
    DateTimeOffset CreatedAt);
