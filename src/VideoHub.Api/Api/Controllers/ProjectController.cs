using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Application.CurrentUser;
using VideoHub.Api.Application.DTOs;
using VideoHub.Api.Infrastructure.Persistence;

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
    [ProducesResponseType(typeof(IEnumerable<ProjectTranscriptResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectTranscript(
        Guid projectId,
        [FromQuery] string? language,
        [FromQuery] int? version,
        [FromServices] IRepository<Project> projectRepository,
        [FromServices] AppDbContext dbContext,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IBlobStorage blobStorage,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return NotFound($"Project '{projectId}' was not found.");

        if (project.UserId != currentUserService.UserId)
            return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this project.");

        if (!string.IsNullOrEmpty(language) && version.HasValue)
        {
            var transcript = await dbContext.Transcripts
                .FirstOrDefaultAsync(t => t.ProjectId == projectId && t.Language == language && t.Version == version.Value, cancellationToken);

            if (transcript is null)
                return NotFound($"No transcript found for project '{projectId}', language '{language}', version '{version}'.");

            var responseUrl = transcript.BlobUrl;
            if (!string.IsNullOrEmpty(responseUrl) && responseUrl.Contains("/storage/v1/object/authenticated/"))
            {
                responseUrl = await blobStorage.GetSignedUrlAsync(responseUrl, TimeSpan.FromHours(1), cancellationToken);
                if (string.IsNullOrEmpty(responseUrl))
                {
                    return StatusCode(StatusCodes.Status502BadGateway, "Unable to access the private storage asset.");
                }
            }

            return Ok(new ProjectTranscriptResponseDto(
                transcript.Id,
                transcript.Language,
                transcript.Status,
                responseUrl,
                transcript.Version));
        }
        else
        {
            var query = dbContext.Transcripts.Where(t => t.ProjectId == projectId);
            if (!string.IsNullOrEmpty(language))
            {
                query = query.Where(t => t.Language == language);
            }
            if (version.HasValue)
            {
                query = query.Where(t => t.Version == version.Value);
            }

            var transcripts = await query.ToListAsync(cancellationToken);

            using var semaphore = new SemaphoreSlim(4);
            var tasks = transcripts.Select(async t =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var responseUrl = t.BlobUrl;
                    if (!string.IsNullOrEmpty(responseUrl) && responseUrl.Contains("/storage/v1/object/authenticated/"))
                    {
                        responseUrl = await blobStorage.GetSignedUrlAsync(responseUrl, TimeSpan.FromHours(1), cancellationToken);
                    }
                    return new { Transcript = t, Url = responseUrl };
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            var dtos = new List<ProjectTranscriptResponseDto>();
            foreach (var r in results)
            {
                var url = r.Url;
                if (!string.IsNullOrEmpty(r.Transcript.BlobUrl) && r.Transcript.BlobUrl.Contains("/storage/v1/object/authenticated/") && string.IsNullOrEmpty(url))
                {
                    return StatusCode(StatusCodes.Status502BadGateway, "Unable to access one or more private storage assets.");
                }
                dtos.Add(new ProjectTranscriptResponseDto(r.Transcript.Id, r.Transcript.Language, r.Transcript.Status, url, r.Transcript.Version));
            }

            return Ok(dtos);
        }
    }
}