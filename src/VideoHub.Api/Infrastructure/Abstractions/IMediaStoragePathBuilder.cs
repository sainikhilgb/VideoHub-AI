namespace VideoHub.Api.Infrastructure.Abstractions;

public interface IMediaStoragePathBuilder
{
    string Build(Guid userId, Guid projectId, string mediaType, string storedFileName);
}
