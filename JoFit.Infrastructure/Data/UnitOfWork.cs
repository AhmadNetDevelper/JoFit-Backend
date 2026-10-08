using JoFit.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;

namespace JoFit.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly JoFitDbContext _joFitDbContext;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction _dbContextTransaction;
    private bool disposed = false;

    public UnitOfWork(JoFitDbContext joFitDbContext, ILogger<UnitOfWork> logger)
    {
        _joFitDbContext = joFitDbContext;
        _logger = logger;
        _repositories = new Dictionary<Type, object>();
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        _dbContextTransaction = await _joFitDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContextTransaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _dbContextTransaction.RollbackAsync(cancellationToken);
            _logger.LogCritical("CommitTransactionAsync {ex}", ex);
            throw;
        }
        finally
        {
            await _dbContextTransaction.DisposeAsync();
            _dbContextTransaction = null;
        }
    }

    public async Task UseTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        // Ensure the context uses the same connection as the transaction
        var dbConnection = _joFitDbContext.Database.GetDbConnection();
        var transactionConnection = transaction.GetDbTransaction().Connection;

        if (dbConnection != transactionConnection)
        {
            throw new InvalidOperationException(
                "The transaction is not associated with the current connection. " +
                "Both DbContext instances must share the same connection to use the same transaction.");
        }

        await _joFitDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);
        _dbContextTransaction = transaction;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_dbContextTransaction != null)
        {
            await _dbContextTransaction.RollbackAsync(cancellationToken);
            await _dbContextTransaction.DisposeAsync();
            _dbContextTransaction = null;
        }   
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _joFitDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
                _logger.LogCritical("Save Changes Async Issue {ex}", ex);
            throw;
        }
    }

    public async Task<int> SaveChangesWithoutEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _joFitDbContext.SaveChangesWithoutEventsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("SaveChangesWithoutEventsAsync Issue {ex}", ex);
            throw;
        }
    }

    public DbContext GetDbContext()
    {
        return _joFitDbContext;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _joFitDbContext.Dispose();
            }
        }

        disposed = true;
    }

    public IGenericRepository<T> GetRepository<T>() where T : class
    {
        if (_repositories.ContainsKey(typeof(T)))
        {
            return _repositories[typeof(T)] as IGenericRepository<T>;
        }

        var repository = new GenericRepository<T>(_joFitDbContext);
        _repositories.Add(typeof(T), repository);
        return repository;
    }
}
