using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> GetRepository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<int> SaveChangesWithoutEventsAsync(CancellationToken cancellationToken);
    Task BeginTransactionAsync(CancellationToken cancellationToken);
    Task CommitTransactionAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
    DbContext GetDbContext();
}
