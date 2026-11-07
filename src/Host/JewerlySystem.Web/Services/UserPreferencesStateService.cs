using MediatR;
using JewerlySystem.Common.Application;
using JewerlySystem.Modules.Auth.Application;

namespace JewerlySystem.Web.Services;

public sealed class UserPreferencesStateService : IUserPreferencesStateService
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    private readonly List<Guid> _favoriteMenuIds = [];
    private List<AppModuleDto> _modules = [];
    private List<string> _moduleOrderKeys = [];
    private string _userId = "system-user";
    private bool _initialized;

    public string ActiveModuleKey { get; private set; } = "CONTRACTS";
    public IReadOnlyList<Guid> FavoriteMenuIds => _favoriteMenuIds;
    public IReadOnlyList<AppModuleDto> Modules => _modules;

    public event Action? OnChange;

    public UserPreferencesStateService(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender;
        _currentUserService = currentUserService;
    }

    public async Task InitializeAsync(string? userId = null)
    {
        var resolvedUserId = !string.IsNullOrWhiteSpace(userId)
            ? userId
            : (_currentUserService.UserId ?? _currentUserService.UserName ?? "system-user");

        if (_initialized && string.Equals(_userId, resolvedUserId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _userId = resolvedUserId;
        _initialized = true;

        try
        {
            var prefResult = await _sender.Send(new GetUserPreferencesQuery(_userId));
            if (prefResult.IsSuccess)
            {
                _favoriteMenuIds.Clear();
                _favoriteMenuIds.AddRange(prefResult.Value.FavoriteMenuIds);
                _moduleOrderKeys = prefResult.Value.ModuleOrderKeys.ToList();
            }

            var modulesResult = await _sender.Send(new GetNavigationModulesQuery(_userId));
            if (modulesResult.IsSuccess && modulesResult.Value.Count > 0)
            {
                _modules = OrderModules(modulesResult.Value, _moduleOrderKeys);

                if (!_modules.Any(m => string.Equals(m.Key, ActiveModuleKey, StringComparison.OrdinalIgnoreCase)))
                {
                    ActiveModuleKey = _modules[0].Key;
                }
            }
        }
        catch
        {
            // Safe fallback during unauthenticated or prerendering state
        }

        NotifyStateChanged();
    }

    public void SetActiveModule(string moduleKey)
    {
        string normalized = string.IsNullOrWhiteSpace(moduleKey) ? "CONTRACTS" : moduleKey;
        if (!string.Equals(ActiveModuleKey, normalized, StringComparison.OrdinalIgnoreCase))
        {
            ActiveModuleKey = normalized;
            NotifyStateChanged();
        }
    }

    public async Task ToggleFavoriteAsync(Guid menuId)
    {
        if (!_favoriteMenuIds.Remove(menuId))
        {
            _favoriteMenuIds.Add(menuId);
        }

        NotifyStateChanged();

        try
        {
            await _sender.Send(new ToggleUserFavoriteCommand(_userId, menuId));
        }
        catch
        {
            // Non-blocking
        }
    }

    public async Task ReorderFavoritesAsync(IReadOnlyList<Guid> orderedMenuIds)
    {
        _favoriteMenuIds.Clear();
        _favoriteMenuIds.AddRange(orderedMenuIds.Distinct());
        NotifyStateChanged();

        try
        {
            await _sender.Send(new ReorderFavoritesCommand(_userId, _favoriteMenuIds));
        }
        catch
        {
            // Non-blocking
        }
    }

    public async Task ReorderModulesAsync(IReadOnlyList<string> orderedModuleKeys)
    {
        _moduleOrderKeys = orderedModuleKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _modules = OrderModules(_modules, _moduleOrderKeys);
        NotifyStateChanged();

        try
        {
            await _sender.Send(new ReorderModulesCommand(_userId, _moduleOrderKeys));
        }
        catch
        {
            // Non-blocking
        }
    }

    public bool IsFavorite(Guid menuId) => _favoriteMenuIds.Contains(menuId);

    private static List<AppModuleDto> OrderModules(IEnumerable<AppModuleDto> modules, List<string> orderKeys)
    {
        var list = modules.ToList();
        if (orderKeys.Count == 0)
        {
            return list.OrderBy(m => m.Order).ToList();
        }

        return list
            .OrderBy(m =>
            {
                int idx = orderKeys.FindIndex(k => string.Equals(k, m.Key, StringComparison.OrdinalIgnoreCase));
                return idx >= 0 ? idx : int.MaxValue;
            })
            .ThenBy(m => m.Order)
            .ToList();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
