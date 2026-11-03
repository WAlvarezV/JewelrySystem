using System.Security.Claims;
using JewerlySystem.Common.Application;
using Microsoft.AspNetCore.Components.Authorization;

namespace JewerlySystem.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authStateProvider;
    private string? _cachedUserName;
    private string? _cachedUserId;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authStateProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _authStateProvider = authStateProvider;
    }

    public string? UserId => _cachedUserId ?? ResolveUserSync().userId;
    public string? UserName => _cachedUserName ?? ResolveUserSync().userName;

    public async ValueTask<string?> GetCurrentUserNameAsync()
    {
        if (!string.IsNullOrEmpty(_cachedUserName))
        {
            return _cachedUserName;
        }

        // 1. Try HttpContext (direct HTTP requests)
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            _cachedUserName = httpUser.Identity.Name;
            _cachedUserId = httpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(_cachedUserName))
            {
                return _cachedUserName;
            }
        }

        // 2. Try Blazor Circuit AuthenticationStateProvider
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                _cachedUserName = authState.User.Identity.Name;
                _cachedUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(_cachedUserName))
                {
                    return _cachedUserName;
                }
            }
        }
        catch
        {
            // fallback
        }

        return "system";
    }

    public string? GetCurrentUserName()
    {
        return ResolveUserSync().userName ?? "system";
    }

    private (string? userId, string? userName) ResolveUserSync()
    {
        if (!string.IsNullOrEmpty(_cachedUserName))
        {
            return (_cachedUserId, _cachedUserName);
        }

        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            _cachedUserName = httpUser.Identity.Name;
            _cachedUserId = httpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(_cachedUserName))
            {
                return (_cachedUserId, _cachedUserName);
            }
        }

        try
        {
            var task = _authStateProvider.GetAuthenticationStateAsync();
            if (task.IsCompletedSuccessfully)
            {
                var user = task.Result.User;
                if (user.Identity?.IsAuthenticated == true)
                {
                    _cachedUserName = user.Identity.Name;
                    _cachedUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    return (_cachedUserId, _cachedUserName);
                }
            }
        }
        catch
        {
            // fallback
        }

        return (null, "system");
    }
}
