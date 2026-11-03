using System.Security.Claims;
using JewerlySystem.Common.Application;
using JewerlySystem.Common.Infrastructure.Tenant;

namespace JewerlySystem.Web.Tenants;

public class WebTenantContext : ITenantContext, ITenantConnectionProvider
{
    private readonly ITenantRegistry _registry;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private TenantInfo? _currentTenant;

    public event Action? OnTenantChanged;

    public WebTenantContext(ITenantRegistry registry, IHttpContextAccessor httpContextAccessor)
    {
        _registry = registry;
        _httpContextAccessor = httpContextAccessor;
    }

    public TenantInfo CurrentTenant
    {
        get
        {
            if (_currentTenant != null)
                return _currentTenant;

            _currentTenant = ResolveInitialTenant();
            return _currentTenant;
        }
    }

    public IReadOnlyList<TenantInfo> AvailableTenants => _registry.GetAllTenants();

    public IReadOnlyList<TenantInfo> GetTenantsForUser(string? userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            var user = _httpContextAccessor.HttpContext?.User;
            userEmail = user?.FindFirstValue(ClaimTypes.Email)
                ?? user?.FindFirstValue(ClaimTypes.Name);
        }

        if (string.IsNullOrWhiteSpace(userEmail))
            return AvailableTenants;

        return AvailableTenants.Where(t => t.CanUserAccess(userEmail)).ToList();
    }

    public bool SetCurrentTenant(string tenantId)
    {
        var target = _registry.GetTenant(tenantId);
        if (target == null) return false;

        _currentTenant = target;

        // Update cookie if HttpContext is available
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && !httpContext.Response.HasStarted)
        {
            httpContext.Response.Cookies.Append("active_tenant", target.Id, new CookieOptions
            {
                Path = "/",
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });

            httpContext.Response.Cookies.Append("pomona_theme", target.DefaultTheme, new CookieOptions
            {
                Path = "/",
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        }

        OnTenantChanged?.Invoke();
        return true;
    }

    public string GetCurrentConnectionString() => CurrentTenant.ConnectionString;

    public string GetConnectionString(string tenantId) => _registry.GetConnectionString(tenantId);

    private TenantInfo ResolveInitialTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        string? cookieTenantId = null;

        if (httpContext != null)
        {
            // 1. Check Query parameter ?tenant=
            if (httpContext.Request.Query.TryGetValue("tenant", out var qVal) && !string.IsNullOrWhiteSpace(qVal))
            {
                cookieTenantId = qVal.ToString();
            }

            // 2. Check Cookie
            if (string.IsNullOrWhiteSpace(cookieTenantId) && httpContext.Request.Cookies.TryGetValue("active_tenant", out var cVal))
            {
                cookieTenantId = cVal;
            }
        }

        if (!string.IsNullOrWhiteSpace(cookieTenantId))
        {
            var match = _registry.GetTenant(cookieTenantId);
            if (match != null)
            {
                return match;
            }
        }

        // Fallback to default tenant
        return _registry.GetDefaultTenant();
    }
}
