using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JoFit.Infrastructure.Data.Interceptors;

/// <summary>
/// Fills the audit columns of <see cref="AuditableEntity"/> automatically on SaveChanges
/// (created / last modified / soft-deleted, at and by).
/// </summary>
public sealed class AuditableEntityInterceptor(IUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var userId = currentUser.Id;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    if (IsSoftDeleted(entry))
                    {
                        entry.Entity.DeletedUtc = now;
                        entry.Entity.DeletedBy = userId;
                    }
                    else
                    {
                        entry.Entity.LastModifiedUtc = now;
                        entry.Entity.LastModifiedBy = userId;
                    }
                    break;
            }
        }
    }

    // A soft delete is an update that flips IsDeleted to true.
    private static bool IsSoftDeleted(EntityEntry<AuditableEntity> entry)
    {
        var isDeleted = entry.Property(x => x.IsDeleted);
        return isDeleted.IsModified && isDeleted.CurrentValue;
    }
}
