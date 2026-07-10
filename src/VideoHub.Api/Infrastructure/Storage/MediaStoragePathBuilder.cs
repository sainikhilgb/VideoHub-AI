using VideoHub.Api.Domain.Media;
using VideoHub.Api.Infrastructure.Abstractions;

namespace VideoHub.Api.Infrastructure.Storage;

public sealed class MediaStoragePathBuilder : IMediaStoragePathBuilder
{
    public string Build(Guid userId, Guid projectId, string mediaType, string storedFileName)
    {
        var folder = mediaType switch
        {
            MediaFileTypes.Video => "original",
            MediaFileTypes.Audio => "audio",
            MediaFileTypes.Document => "original",
            _ => "original"
        };

        return $"{userId}/{projectId}/{folder}/{storedFileName}";
    }
}
