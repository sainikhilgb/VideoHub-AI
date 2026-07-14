using Microsoft.Extensions.Logging;
using VideoHub.Api.Application.CurrentUser;
using VideoHub.Api.Application.Exceptions;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Infrastructure.Abstractions;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IProjectRepository projectRepository,
        IRepository<User> userRepository,
        ICurrentUserService currentUserService,
        ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(ProjectRequestDto project, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        _logger.LogInformation("Project Creation Started: Name={Name} UserId={UserId}", project.Name, currentUserId);

        User? user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException($"User '{currentUserId}' was not found.");
        }

        var newProject = await _projectRepository.CreateAsync(new Project
        {
            Name = project.Name,
            OriginalLanguage = project.OriginalLanguage,
            UserId = currentUserId,
            User = user,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        _logger.LogInformation("Project Creation Completed: ProjectId={ProjectId}", newProject.Id);

        return MapToResponse(newProject);
    }

    public async Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        _logger.LogInformation("Project Deletion Started: ProjectId={ProjectId} UserId={UserId}", id, currentUserId);

        var existingProject = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (existingProject is null)
            throw new NotFoundException($"Project '{id}' was not found.");

        if (existingProject.UserId != currentUserId)
            throw new ForbiddenException("You do not have permission to delete this project.");

        await _projectRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Project Deletion Completed: ProjectId={ProjectId}", id);
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        _logger.LogInformation("Project Retrieval Started: UserId={UserId}", currentUserId);

        var projects = await _projectRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        var result = projects.Select(MapToResponse).ToList();

        _logger.LogInformation("Project Retrieval Completed: Count={Count}", result.Count);

        return result;
    }

    public async Task<ProjectResponseDto> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        _logger.LogInformation("Project Retrieval Started: ProjectId={ProjectId} UserId={UserId}", id, currentUserId);

        var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (project is null)
            throw new NotFoundException($"Project '{id}' was not found.");

        if (project.UserId != currentUserId)
            throw new ForbiddenException("You do not have permission to access this project.");

        _logger.LogInformation("Project Retrieval Completed: ProjectId={ProjectId}", id);

        return MapToResponse(project);
    }

    public async Task UpdateProjectAsync(Guid id, ProjectRequestDto project, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;
        _logger.LogInformation("Project Update Started: ProjectId={ProjectId} UserId={UserId}", id, currentUserId);

        var existingProject = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (existingProject is null)
            throw new NotFoundException($"Project '{id}' was not found.");

        if (existingProject.UserId != currentUserId)
            throw new ForbiddenException("You do not have permission to modify this project.");

        existingProject.Name = project.Name;
        existingProject.UpdatedAt = DateTimeOffset.UtcNow;

        await _projectRepository.UpdateAsync(id, existingProject, cancellationToken);

        _logger.LogInformation("Project Update Completed: ProjectId={ProjectId}", id);
    }

    private static ProjectResponseDto MapToResponse(Project project)
    {
        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            OriginalLanguage = project.OriginalLanguage,
            UserId = project.UserId,
            Status = project.Status,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}
