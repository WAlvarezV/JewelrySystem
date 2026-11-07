using JewerlySystem.Common.Application;

namespace JewerlySystem.Modules.Auth.Application;

public record LoginResponseDto(
    bool Succeeded,
    bool RequiresTwoFactor,
    bool IsLockedOut,
    string? UserId = null,
    string? Message = null);

public record MfaSetupDto(
    string SharedKey,
    string AuthenticatorUri,
    string QrCodeBase64);

public record MfaStatusDto(
    bool IsEnabled,
    bool HasAuthenticatorKey,
    int RecoveryCodesLeft);

public record LoginWithPasswordCommand(string EmailOrUserName, string Password, bool RememberMe = false)
    : ICommand<LoginResponseDto>;

public record LoginWithMfaCommand(string TwoFactorCode, bool RememberMachine = false)
    : ICommand<bool>;

public record GenerateMfaQrCodeQuery(string UserId)
    : IQuery<MfaSetupDto>;

public record GetMfaStatusQuery(string UserId)
    : IQuery<MfaStatusDto>;

public record EnableMfaCommand(string UserId, string Code)
    : ICommand<IReadOnlyList<string>>;

public record DisableMfaCommand(string UserId)
    : ICommand<bool>;

public record GenerateNewRecoveryCodesCommand(string UserId)
    : ICommand<IReadOnlyList<string>>;

public record GetPasskeyAssertionOptionsQuery(string? Username)
    : IQuery<string>;

public record VerifyPasskeyAssertionCommand(string AssertionResponseJson)
    : ICommand<string>;

public record GetPasskeyCreationOptionsQuery(string UserId, string DisplayName)
    : IQuery<string>;

public record RegisterPasskeyCommand(string UserId, string KeyName, string AttestationResponseJson)
    : ICommand<bool>;

public record PasskeyItemDto(Guid Id, string Name, DateTime CreatedAt, DateTime? LastUsedAt);

public record GetPasskeysByUserQuery(string UserId) : IQuery<IReadOnlyList<PasskeyItemDto>>;

public record DeletePasskeyCommand(Guid Id, string UserId) : ICommand<bool>;

// ==========================================
// DTOs & CQRS for Modules, Menus & Favorites
// ==========================================

public record AppModuleDto(
    Guid Id,
    string Key,
    string Name,
    string Icon,
    string Description,
    int Order,
    bool IsActive);

public class MenuDto
{
    public Guid Id { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RequiredPermission { get; set; }
    public List<MenuDto> Children { get; set; } = [];
}

public record GetNavigationModulesQuery(string? UserId = null) : IQuery<IReadOnlyList<AppModuleDto>>;

public record GetNavigationMenusQuery(string? ModuleKey = null, string? UserId = null) : IQuery<IReadOnlyList<MenuDto>>;

public record GetUserFavoritesQuery(string UserId) : IQuery<IReadOnlyList<MenuDto>>;

public record ToggleUserFavoriteCommand(string UserId, Guid MenuId) : ICommand<bool>;

public record UserPreferencesDto(
    IReadOnlyList<Guid> FavoriteMenuIds,
    IReadOnlyList<string> ModuleOrderKeys);

public record GetUserPreferencesQuery(string UserId) : IQuery<UserPreferencesDto>;

public record SaveUserPreferencesCommand(
    string UserId,
    IReadOnlyList<Guid> FavoriteMenuIds,
    IReadOnlyList<string> ModuleOrderKeys) : ICommand<bool>;

public record ReorderFavoritesCommand(string UserId, IReadOnlyList<Guid> OrderedMenuIds) : ICommand<bool>;

public record ReorderModulesCommand(string UserId, IReadOnlyList<string> OrderedModuleKeys) : ICommand<bool>;

public record CreateModuleCommand(string Key, string Name, string Icon, string Description, int Order) : ICommand<Guid>;

public record UpdateModuleCommand(Guid Id, string Key, string Name, string Icon, string Description, int Order, bool IsActive) : ICommand<bool>;

public record ToggleModuleStatusCommand(Guid Id) : ICommand<bool>;

public record CreateMenuCommand(string ModuleKey, string Name, string Route, string Icon, int Order, Guid? ParentId = null) : ICommand<Guid>;

public record UpdateMenuCommand(Guid Id, string ModuleKey, string Name, string Route, string Icon, int Order, bool IsActive, Guid? ParentId = null) : ICommand<bool>;

public record ToggleMenuStatusCommand(Guid Id) : ICommand<bool>;

// ==========================================
// DTOs & CQRS for Users, Roles & Permissions
// ==========================================

public record UserItemDto(
    string Id,
    string UserName,
    string Email,
    string PhoneNumber,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool TwoFactorEnabled,
    int PasskeyCount,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record GetAllUsersQuery() : IQuery<IReadOnlyList<UserItemDto>>;

public record CreateUserCommand(string UserName, string Email, string Password, string Role, string PhoneNumber) : ICommand<string>;

public record ToggleUserStatusCommand(string UserId) : ICommand<bool>;

public record AssignUserRoleCommand(string UserId, string Role) : ICommand<bool>;

public record RoleItemDto(
    string Id,
    string Name,
    string Description,
    int UserCount);

public record GetAllRolesQuery() : IQuery<IReadOnlyList<RoleItemDto>>;

public record CreateRoleCommand(string Name, string Description) : ICommand<string>;

public record PermissionItemDto(
    Guid Id,
    string RoleName,
    string ModuleKey,
    string MenuRoute,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete,
    bool CanAdmin);

public record GetPermissionsByRoleQuery(string RoleName) : IQuery<IReadOnlyList<PermissionItemDto>>;

public record SavePermissionsCommand(string RoleName, IReadOnlyList<PermissionItemDto> Permissions) : ICommand<bool>;

// ==========================================
// DTOs & CQRS for User Profile & Password Change
// ==========================================

public record UserProfileDto(
    string Id,
    string UserName,
    string Email,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    bool TwoFactorEnabled,
    int PasskeyCount,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    string FirstName = "",
    string FirstLastName = "",
    string? SecondLastName = null,
    int? IdentificationTypeId = null,
    string IdentificationNumber = "",
    string? Address = null,
    DateTime? BirthDate = null,
    DateTime? HireDate = null);

public record GetUserProfileQuery(string UserIdOrEmail) : IQuery<UserProfileDto>;

public record ChangeUserPasswordCommand(
    string UserIdOrEmail,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : ICommand<bool>;

public record UpdateUserProfileCommand(
    string UserIdOrEmail,
    string? PhoneNumber) : ICommand<bool>;

public record UpdateUserPersonalDataCommand(
    string UserIdOrEmail,
    string FirstName,
    string FirstLastName,
    string? SecondLastName,
    int? IdentificationTypeId,
    string IdentificationNumber,
    string? Address,
    string? PhoneNumber,
    DateTime? BirthDate,
    DateTime? HireDate) : ICommand<bool>;
