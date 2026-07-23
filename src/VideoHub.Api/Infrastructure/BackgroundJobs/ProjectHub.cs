using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Application.CurrentUser;

namespace VideoHub.Api.Infrastructure.BackgroundJobs;

[Authorize]
public sealed class ProjectHub : Hub
{
    private readonly IRepository<Project> projectRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly ILogger<ProjectHub> logger;

    public ProjectHub(
        IRepository<Project> projectRepository,
        ICurrentUserService currentUserService,
        ILogger<ProjectHub> logger)
    {
        this.projectRepository = projectRepository;
        this.currentUserService = currentUserService;
        this.logger = logger;
    }

    public async Task JoinProjectGroup(string projectId)
    {
        if (Guid.TryParse(projectId, out var parsedProjectId))
        {
            var project = await projectRepository.GetByIdAsync(parsedProjectId, Context.ConnectionAborted);
            if (project is null)
            {
                logger.LogWarning("Client {ConnectionId} attempted to join project group for non-existent project: {ProjectId}", Context.ConnectionId, parsedProjectId);
                return;
            }

            if (project.UserId != currentUserService.UserId)
            {
                logger.LogWarning("Client {ConnectionId} (User {UserId}) unauthorized to join project group for Project {ProjectId} (Owner {OwnerId})", 
                    Context.ConnectionId, currentUserService.UserId, parsedProjectId, project.UserId);
                return;
            }

            var groupName = $"project_{parsedProjectId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            logger.LogInformation("Client {ConnectionId} joined project group {GroupName}", Context.ConnectionId, groupName);
        }
        else
        {
            logger.LogWarning("Client {ConnectionId} attempted to join invalid project group: {ProjectId}", Context.ConnectionId, projectId);
        }
    }

    public async Task LeaveProjectGroup(string projectId)
    {
        if (Guid.TryParse(projectId, out var parsedProjectId))
        {
            var groupName = $"project_{parsedProjectId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            logger.LogInformation("Client {ConnectionId} left project group {GroupName}", Context.ConnectionId, groupName);
        }
    }

    public override Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected to ProjectHub: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Client disconnected from ProjectHub: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
