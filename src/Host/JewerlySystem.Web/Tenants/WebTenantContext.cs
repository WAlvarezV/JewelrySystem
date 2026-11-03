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
        string? candidateTenantId = null;

        if (httpContext != null)
        {
            // 1. Check Query parameter ?tenant=
            if (httpContext.Request.Query.TryGetValue("tenant", out var qVal) && !string.IsNullOrWhiteSpace(qVal))
            {
                candidateTenantId = qVal.ToString();
            }

            // 2. Check Path segment (e.g. /pomona/login or /oro-plata/login)
            if (string.IsNullOrWhiteSpace(candidateTenantId))
            {
                var path = httpContext.Request.Path.Value ?? "";
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    var firstSegment = segments[0];
                    if (_registry.GetTenant(firstSegment) != null)
                    {
                        candidateTenantId = firstSegment;
                    }
                }
            }

            // 3. Check Header (X-Tenant-Id)
            if (string.IsNullOrWhiteSpace(candidateTenantId) && httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var hVal) && !string.IsNullOrWhiteSpace(hVal))
            {
                candidateTenantId = hVal.ToString();
            }

            // 4. Check Cookie (active_tenant)
            if (string.IsNullOrWhiteSpace(candidateTenantId) && httpContext.Request.Cookies.TryGetValue("active_tenant", out var cVal))
            {
                candidateTenantId = cVal;
            }
        }

        if (!string.IsNullOrWhiteSpace(candidateTenantId))
        {
            var match = _registry.GetTenant(candidateTenantId);
            if (match != null)
            {
                // Sync cookies if headers not yet sent
                if (httpContext != null && !httpContext.Response.HasStarted)
                {
                    try
                    {
                        var cookieOptions = new CookieOptions
                        {
                            Path = "/",
                            HttpOnly = false,
                            SameSite = SameSiteMode.Lax,
                            Secure = httpContext.Request.IsHttps,
                            Expires = DateTimeOffset.UtcNow.AddYears(1)
                        };
                        httpContext.Response.Cookies.Append("active_tenant", match.Id, cookieOptions);
                        httpContext.Response.Cookies.Append("pomona_theme", match.DefaultTheme, cookieOptions);
                    }
                    catch
                    {
                        // Ignore if response already committed
                    }
                }

                return match;
            }
        }

        // Fallback to default tenant
        return _registry.GetDefaultTenant();
    }
}
