using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Plugins.PluginConfigurations.PluginConfigurationAggregate;
using Microsoft.EntityFrameworkCore;

namespace Grants.ApplicantPortal.API.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for plugin configurations
/// </summary>
public class PluginConfigurationRepository : IPluginConfigurationRepository
{
    private readonly AppDbContext _dbContext;

    public PluginConfigurationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PluginConfiguration?> GetByPluginIdAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PluginConfiguration>()
            .FirstOrDefaultAsync(p => p.PluginId == pluginId && p.IsActive, cancellationToken);
    }

    public async Task<PluginConfiguration> AddAsync(PluginConfiguration entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<PluginConfiguration>().Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(PluginConfiguration entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<PluginConfiguration>().Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PluginConfiguration entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<PluginConfiguration>().Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PluginConfiguration?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        if (id is Guid guidId)
        {
            return await _dbContext.Set<PluginConfiguration>()
                .FirstOrDefaultAsync(p => p.Id == guidId, cancellationToken);
        }
        return null;
    }

    public async Task<List<PluginConfiguration>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PluginConfiguration>()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PluginConfiguration>> ListAsync(ISpecification<PluginConfiguration> specification, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PluginConfiguration>()
            .WithSpecification(specification)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(ISpecification<PluginConfiguration> specification, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PluginConfiguration>()
            .WithSpecification(specification)
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PluginConfiguration>()
            .CountAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(ISpecification<PluginConfiguration> specification, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PluginConfiguration>()
            .WithSpecification(specification)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PluginConfiguration>()
            .AnyAsync(cancellationToken);
    }

    public async Task<PluginConfiguration?> FirstOrDefaultAsync(ISpecification<PluginConfiguration> specification, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PluginConfiguration>()
            .WithSpecification(specification)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PluginConfiguration?> SingleOrDefaultAsync(ISingleResultSpecification<PluginConfiguration> specification, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PluginConfiguration>()
            .WithSpecification(specification)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
