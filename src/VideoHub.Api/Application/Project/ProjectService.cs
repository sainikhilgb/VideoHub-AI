
using VideoHub.Api.Domain.Entities;
using static VideoHub.Api.Infrastructure.Middleware.ExceptionHandlingMiddleware;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(ProjectRequestDto project)
    {
        var newProject = await _projectRepository.CreateAsync(new Project
        {
            Name = project.Name,
            OriginalLanguage = project.OriginalLanguage,
            UserId = project.UserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        return new ProjectResponseDto
        {
            Id = newProject.Id,
            Name = newProject.Name,
            OriginalLanguage = newProject.OriginalLanguage,
            UserId = newProject.UserId,
            Status = newProject.Status,
            CreatedAt = newProject.CreatedAt,
            UpdatedAt = newProject.UpdatedAt
        };
    }

    public async Task<bool> DeleteProjectAsync(Guid id)
    {
        return await _projectRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetAllProjectsAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        return projects.Select(p => new ProjectResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            OriginalLanguage = p.OriginalLanguage,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        });
    }

    public async Task<ProjectResponseDto> GetProjectByIdAsync(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project is null)
            throw new NotFoundException("Project not found");

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            OriginalLanguage = project.OriginalLanguage,
            Status = project.Status,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    public async Task<bool> UpdateProjectAsync(Guid id, ProjectRequestDto project)
    {
        var existingProject = await _projectRepository.GetByIdAsync(id);
        if (existingProject is null)
            throw new NotFoundException("Project not found");

        existingProject.Name = project.Name;
        existingProject.UpdatedAt = DateTimeOffset.UtcNow;

        return await _projectRepository.UpdateAsync(id, existingProject);
    }
}
