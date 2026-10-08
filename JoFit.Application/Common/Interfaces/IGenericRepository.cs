using System.Linq.Expressions;

namespace JoFit.Application.Common.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T> AddAsync(T entity, CancellationToken ct);
    Task AddRangeAsync(ICollection<T> entities, CancellationToken ct);
    void UpdateAsync(T entity);
    void DeleteAsync(T entity, bool softDelete = true);
    void UpdateRangeAsync(ICollection<T> entities);
    void DeleteRangeAsync(ICollection<T> entities, bool softDelete = true);
    Task<T> GetByIdAsync(long id, CancellationToken ct);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<T>> Where(Expression<Func<T, bool>> expression);
    IQueryable<T> GetTableNoTracking();
    IQueryable<T> GetTableAsTracking();
}
