using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace VideoHub.Api.Infrastructure.BackgroundJobs;

[Authorize]
public sealed class ProjectHub : Hub
{
    private readonly ILogger<ProjectHub> logger;

    public ProjectHub(ILogger<ProjectHub> logger)
    {
        this.logger = logger;
    }

    public async Task JoinProjectGroup(string projectId)
    {
        if (Guid.TryParse(projectId, out _))
        {
            var groupName = $"project_{projectId}";
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
        if (Guid.TryParse(projectId, out _))
        {
            var groupName = $"project_{projectId}";
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
