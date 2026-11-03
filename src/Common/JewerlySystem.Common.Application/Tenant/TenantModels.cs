namespace JewerlySystem.Common.Application;

public record TenantInfo(
    string Id,
    string Name,
    string ShortName,
    string ConnectionString,
    string DefaultTheme,
    string SymbolUrl,
    string LogoUrl,
    IReadOnlyList<string> AllowedUserEmails
)
{
    public bool CanUserAccess(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        if (AllowedUserEmails.Count == 0) return true; // Open if none specified
        return AllowedUserEmails.Any(u => string.Equals(u, email, StringComparison.OrdinalIgnoreCase) || u == "*");
    }
}

public interface ITenantContext
{
    TenantInfo CurrentTenant { get; }
    IReadOnlyList<TenantInfo> AvailableTenants { get; }
    IReadOnlyList<TenantInfo> GetTenantsForUser(string? userEmail);
    bool SetCurrentTenant(string tenantId);
    event Action? OnTenantChanged;
}

public interface ITenantConnectionProvider
{
    string GetCurrentConnectionString();
    string GetConnectionString(string tenantId);
}
