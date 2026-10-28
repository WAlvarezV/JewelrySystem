using JewerlySystem.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JewerlySystem.Common.Infrastructure;

public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditableEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateAuditableEntities(DbContext context)
    {
        var utcNow = DateTime.UtcNow;
        var entries = context.ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is AuditableEntity<int> auditableInt)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableInt.CreatedAt = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditableInt.LastModifiedAt = utcNow;
                }
            }
        }
    }
}
