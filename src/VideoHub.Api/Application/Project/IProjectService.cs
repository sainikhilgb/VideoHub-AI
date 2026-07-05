using VideoHub.Api.Domain.Entities;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponseDto>> GetAllProjectsAsync();
    Task<ProjectResponseDto> GetProjectByIdAsync(Guid id);
    Task<ProjectResponseDto> CreateProjectAsync(ProjectRequestDto project);
    Task<bool> UpdateProjectAsync(Guid id, ProjectRequestDto project);
    Task<bool> DeleteProjectAsync(Guid id);
}
