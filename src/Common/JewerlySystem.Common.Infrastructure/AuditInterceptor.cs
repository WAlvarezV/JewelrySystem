using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JewerlySystem.Common.Infrastructure;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService? _currentUserService;

    public AuditInterceptor(ICurrentUserService? currentUserService = null)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            var currentUser = _currentUserService?.GetCurrentUserName() ?? "system";
            UpdateAuditableEntities(eventData.Context, currentUser);
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var currentUser = _currentUserService != null
                ? await _currentUserService.GetCurrentUserNameAsync()
                : "system";

            UpdateAuditableEntities(eventData.Context, currentUser ?? "system");
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateAuditableEntities(DbContext context, string currentUser)
    {
        var utcNow = DateTime.UtcNow;
        var entries = context.ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = utcNow;
                    if (string.IsNullOrWhiteSpace(auditable.CreatedBy))
                    {
                        auditable.CreatedBy = currentUser;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.LastModifiedAt = utcNow;
                    auditable.LastModifiedBy = currentUser;

                    // Preserve original creation audit info
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;
                }
            }
        }
    }
}
