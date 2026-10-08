using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace JoFit.Infrastructure.Data;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly JoFitDbContext _joFitDbContext;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(JoFitDbContext joFitDbContext)
    {
        _joFitDbContext = joFitDbContext;
        _dbSet = joFitDbContext.Set<T>();
    }

    public async Task<T> AddAsync(T entity, CancellationToken ct)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public async Task AddRangeAsync(ICollection<T> entities, CancellationToken ct)
    {
        await _dbSet.AddRangeAsync(entities, ct);
    }

    public void DeleteAsync(T entity, bool softDelete = true)
    {
        if (softDelete)
        {
            if (entity is AuditableEntity softDeleteEntity)
            {
                softDeleteEntity.IsDeleted = true;
                _dbSet.Update(entity);
            }
        }
        else
        {
            _dbSet.Remove(entity);
        }
    }

    public void DeleteRangeAsync(ICollection<T> entities, bool softDelete = true)
    {
        if (softDelete)
        {
            foreach (var entity in entities)
            {
                if (entity is AuditableEntity softDeleteEntity)
                {
                    softDeleteEntity.IsDeleted = true;
                    _dbSet.Update(entity);
                }
            }
        }
        else
        {
            _dbSet.RemoveRange(entities);
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public async Task<T> GetByIdAsync(long id, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking().SingleOrDefaultAsync(e => EF.Property<long>(e, "Id") == id, ct);
    }

    public IQueryable<T> GetTableAsTracking()
    {
        return _dbSet.AsQueryable();
    }

    public IQueryable<T> GetTableNoTracking()
    {
        return _dbSet.AsNoTracking().AsQueryable();
    }

    public void UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
    }

    public void UpdateRangeAsync(ICollection<T> entities)
    {
        foreach (var entity in entities)
        {
            _dbSet.Update(entity);
        }
    }

    public async Task<IEnumerable<T>> Where(Expression<Func<T, bool>> expression)
    {
        return await _dbSet.AsNoTracking().Where(expression).ToListAsync();
    }

    /// <summary>
    /// Retrieves only the deleted records (no tracking).
    /// Useful for viewing audit trails or soft-deleted items.
    /// </summary>
    public async Task<IEnumerable<T>> GetDeletedOnlyAsync(CancellationToken ct)
    {
        if (typeof(AuditableEntity).IsAssignableFrom(typeof(T)))
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
            var lambda = Expression.Lambda<Func<T, bool>>(property, parameter);

            return await _dbSet.IgnoreQueryFilters().AsNoTracking().Where(lambda).ToListAsync(ct);
        }

        return new List<T>();
    }
}