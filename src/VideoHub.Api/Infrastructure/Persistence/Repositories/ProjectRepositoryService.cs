using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Infrastructure.Persistence;

namespace VideoHub.Api.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepositoryService : IProjectRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectRepositoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Project>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project> CreateAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Projects.AddAsync(project, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return project;
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        Project project,
        CancellationToken cancellationToken = default)
    {
        var existingProject = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (existingProject is null)
            return false;

        existingProject.Name = project.Name;
        existingProject.UpdatedAt = project.UpdatedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
            return false;

        _dbContext.Projects.Remove(project);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}