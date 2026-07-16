using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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
        [FromServices] IConfiguration configuration,
        [FromServices] ILogger<ProjectController> logger,
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
                responseUrl = await GenerateSignedUrlAsync(responseUrl, configuration, logger, cancellationToken);
            }

            return Ok(new ProjectTranscriptResponseDto(
                transcript.Id,
                transcript.Language,
                transcript.Status,
                responseUrl));
        }
        else
        {
            var transcripts = await dbContext.Transcripts
                .Where(t => t.ProjectId == projectId)
                .ToListAsync(cancellationToken);

            var dtos = new List<ProjectTranscriptResponseDto>();
            foreach (var t in transcripts)
            {
                var responseUrl = t.BlobUrl;
                if (!string.IsNullOrEmpty(responseUrl) && responseUrl.Contains("/storage/v1/object/authenticated/"))
                {
                    responseUrl = await GenerateSignedUrlAsync(responseUrl, configuration, logger, cancellationToken);
                }
                dtos.Add(new ProjectTranscriptResponseDto(t.Id, t.Language, t.Status, responseUrl));
            }

            return Ok(dtos);
        }
    }

    private async Task<string> GenerateSignedUrlAsync(
        string privateUrl,
        IConfiguration configuration,
        ILogger<ProjectController> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var uri = new Uri(privateUrl);
            var pathAndQuery = uri.AbsolutePath;
            var prefix = "/storage/v1/object/authenticated/";
            if (pathAndQuery.Contains(prefix))
            {
                var index = pathAndQuery.IndexOf(prefix);
                var relativePath = pathAndQuery.Substring(index + prefix.Length);
                var parts = relativePath.Split('/', 2);
                if (parts.Length == 2)
                {
                    var bucket = parts[0];
                    var objectPath = parts[1];

                    var supabaseUrl = configuration["BlobStorage:SupabaseUrl"];
                    var supabaseKey = configuration["BlobStorage:SupabaseKey"];

                    if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                    {
                        logger.LogWarning("Supabase credentials are not configured in the host environment.");
                        return privateUrl;
                    }

                    using var client = new HttpClient();
                    client.BaseAddress = new Uri(supabaseUrl.TrimEnd('/') + "/");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
                    client.DefaultRequestHeaders.Add("apikey", supabaseKey);

                    var response = await client.PostAsJsonAsync(
                        $"storage/v1/object/sign/{Uri.EscapeDataString(bucket)}/{objectPath}",
                        new { expiresIn = 3600 },
                        cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<SupabaseSignedUrlResponse>(cancellationToken);
                        if (result != null && !string.IsNullOrEmpty(result.SignedURL))
                        {
                            var signedPath = result.SignedURL;
                            if (signedPath.StartsWith("/"))
                            {
                                return $"{supabaseUrl.TrimEnd('/')}{signedPath}";
                            }
                            return signedPath;
                        }
                    }
                    else
                    {
                        var err = await response.Content.ReadAsStringAsync(cancellationToken);
                        logger.LogWarning("Supabase sign request failed: Status={Status} Body={Body}", response.StatusCode, err);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate signed URL for {Url}", privateUrl);
        }

        return privateUrl;
    }

    private sealed class SupabaseSignedUrlResponse
    {
        [JsonPropertyName("signedURL")]
        public string SignedURL { get; set; } = string.Empty;
    }
}