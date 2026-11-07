using JewerlySystem.Modules.Auth.Application;

namespace JewerlySystem.Web.Services;

public interface IUserPreferencesStateService
{
    string ActiveModuleKey { get; }
    IReadOnlyList<Guid> FavoriteMenuIds { get; }
    IReadOnlyList<AppModuleDto> Modules { get; }

    event Action? OnChange;

    Task InitializeAsync(string? userId = null);
    void SetActiveModule(string moduleKey);
    Task ToggleFavoriteAsync(Guid menuId);
    Task ReorderFavoritesAsync(IReadOnlyList<Guid> orderedMenuIds);
    Task ReorderModulesAsync(IReadOnlyList<string> orderedModuleKeys);
    bool IsFavorite(Guid menuId);
}
