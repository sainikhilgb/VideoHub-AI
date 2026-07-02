using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Infrastructure.Abstractions;

namespace VideoHub.Api.Infrastructure.Persistence.Repositories;

public sealed class EfRepository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    private readonly AppDbContext dbContext;

    public EfRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Set<TEntity>().ToListAsync(cancellationToken);

    public void Remove(TEntity entity) => dbContext.Set<TEntity>().Remove(entity);

    public void Update(TEntity entity) => dbContext.Set<TEntity>().Update(entity);
}
