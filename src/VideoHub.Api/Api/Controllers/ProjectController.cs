using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Application.CurrentUser;
using VideoHub.Api.Application.DTOs;

namespace VideoHub.Api.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/projects")]
public sealed class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProjects(CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetAllProjectsAsync(cancellationToken);
        return Ok(projects);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectById(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetProjectByIdAsync(id, cancellationToken);
        return Ok(project);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProject([FromBody] ProjectRequestDto project, CancellationToken cancellationToken)
    {
        var createdProject = await _projectService.CreateProjectAsync(project, cancellationToken);
        return CreatedAtAction(nameof(GetProjectById), new { id = createdProject.Id }, createdProject);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] ProjectRequestDto project, CancellationToken cancellationToken)
    {
        await _projectService.UpdateProjectAsync(id, project, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        await _projectService.DeleteProjectAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieves the speech transcript associated with the specified project.
    /// </summary>
    [HttpGet("{projectId:guid}/transcript")]
    [ProducesResponseType(typeof(ProjectTranscriptResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectTranscript(
        Guid projectId,
        [FromServices] IRepository<Project> projectRepository,
        [FromServices] IRepository<Transcript> transcriptRepository,
        [FromServices] ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return NotFound($"Project '{projectId}' was not found.");

        if (project.UserId != currentUserService.UserId)
            return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this project.");

        var allTranscripts = await transcriptRepository.ListAsync(cancellationToken);
        var transcript = allTranscripts.FirstOrDefault(t => t.ProjectId == projectId);

        if (transcript is null)
            return NotFound($"No transcript found for project '{projectId}'.");

        return Ok(new ProjectTranscriptResponseDto(
            transcript.Id,
            transcript.Language,
            transcript.Status,
            transcript.BlobUrl));
    }
}