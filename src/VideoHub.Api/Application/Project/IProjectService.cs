public interface IProjectService
{
    Task<IEnumerable<ProjectResponseDto>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
    Task<ProjectResponseDto> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectResponseDto> CreateProjectAsync(ProjectRequestDto project, CancellationToken cancellationToken = default);
    Task UpdateProjectAsync(Guid id, ProjectRequestDto project, CancellationToken cancellationToken = default);
    Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default);
}
