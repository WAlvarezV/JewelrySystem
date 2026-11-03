using System.Text.Json;
using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Auth.Application;
using JewerlySystem.Modules.Auth.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Auth.Infrastructure;

public class NavigationAndSecurityHandlers :
    IQueryHandler<GetNavigationModulesQuery, IReadOnlyList<AppModuleDto>>,
    IQueryHandler<GetNavigationMenusQuery, IReadOnlyList<MenuDto>>,
    IQueryHandler<GetUserFavoritesQuery, IReadOnlyList<MenuDto>>,
    ICommandHandler<ToggleUserFavoriteCommand, bool>,
    ICommandHandler<CreateModuleCommand, Guid>,
    ICommandHandler<UpdateModuleCommand, bool>,
    ICommandHandler<ToggleModuleStatusCommand, bool>,
    ICommandHandler<CreateMenuCommand, Guid>,
    ICommandHandler<UpdateMenuCommand, bool>,
    ICommandHandler<ToggleMenuStatusCommand, bool>,
    IQueryHandler<GetAllUsersQuery, IReadOnlyList<UserItemDto>>,
    ICommandHandler<CreateUserCommand, string>,
    ICommandHandler<ToggleUserStatusCommand, bool>,
    ICommandHandler<AssignUserRoleCommand, bool>,
    IQueryHandler<GetAllRolesQuery, IReadOnlyList<RoleItemDto>>,
    ICommandHandler<CreateRoleCommand, string>,
    IQueryHandler<GetPermissionsByRoleQuery, IReadOnlyList<PermissionItemDto>>,
    ICommandHandler<SavePermissionsCommand, bool>,
    IQueryHandler<GetPasskeysByUserQuery, IReadOnlyList<PasskeyItemDto>>,
    ICommandHandler<DeletePasskeyCommand, bool>
{
    private readonly AuthDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _seededTenants = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim _seedLock = new(1, 1);

    public NavigationAndSecurityHandlers(
        AuthDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    private static readonly SemaphoreSlim _dbLock = new(1, 1);

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        await _dbLock.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task EnsureNavigationSeededAsync(CancellationToken ct)
    {
        var dbName = _dbContext.Database.GetDbConnection().Database ?? "default";
        if (_seededTenants.ContainsKey(dbName))
        {
            return;
        }

        await _seedLock.WaitAsync(ct);
        try
        {
            if (_seededTenants.ContainsKey(dbName))
            {
                return;
            }

            // 1. Seed Modules and Menus if missing
        if (!await _dbContext.AppModules.AnyAsync(ct))
        {
            var defaultModules = new List<AppModule>
            {
                new() { Id = Guid.NewGuid(), Key = "CONTRACTS", Name = "Contratos", Icon = "bi-file-earmark-text", Description = "Gestión de contratos de compraventa y empeño", Order = 1, IsActive = true },
                new() { Id = Guid.NewGuid(), Key = "INVENTORY", Name = "Inventario", Icon = "bi-box-seam", Description = "Control de stock de joyas y relojería", Order = 2, IsActive = true },
                new() { Id = Guid.NewGuid(), Key = "CUSTOMERS", Name = "Clientes", Icon = "bi-people", Description = "Directorio y clientes", Order = 3, IsActive = true },
                new() { Id = Guid.NewGuid(), Key = "CASH", Name = "Caja", Icon = "bi-cash-stack", Description = "Arqueo y control diario de caja", Order = 4, IsActive = true },
                new() { Id = Guid.NewGuid(), Key = "INVOICING", Name = "Facturación", Icon = "bi-receipt", Description = "Emisión y control de facturas", Order = 5, IsActive = true },
                new() { Id = Guid.NewGuid(), Key = "ADMIN", Name = "Seguridad", Icon = "bi-shield-lock", Description = "Usuarios, módulos, menús, roles y permisos", Order = 6, IsActive = true }
            };

            var defaultMenus = new List<MenuItem>
            {
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), ModuleKey = "CONTRACTS", Name = "Todos los Contratos", Route = "/contracts", Icon = "bi-file-earmark-text", Order = 1, IsActive = true },
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111112"), ModuleKey = "CONTRACTS", Name = "Nuevo Contrato", Route = "/contracts/new", Icon = "bi-plus-circle", Order = 2, IsActive = true },
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111113"), ModuleKey = "CONTRACTS", Name = "Calculadora Ley Oro", Route = "/utilities/gold-calculator", Icon = "bi-calculator", Order = 3, IsActive = true },

                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), ModuleKey = "INVENTORY", Name = "Catálogo de Joyas y Relojes", Route = "/inventory", Icon = "bi-box-seam", Order = 1, IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222223"), ModuleKey = "INVENTORY", Name = "Nueva Joya", Route = "/inventory/jewels/new", Icon = "bi-gem", Order = 2, IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222224"), ModuleKey = "INVENTORY", Name = "Nuevo Reloj", Route = "/inventory/watches/new", Icon = "bi-watch", Order = 3, IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222225"), ModuleKey = "INVENTORY", Name = "Descargar Artículo", Route = "/inventory/discharge", Icon = "bi-box-arrow-right", Order = 4, IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222226"), ModuleKey = "INVENTORY", Name = "Consolidado de Inventario", Route = "/inventory/consolidated", Icon = "bi-pie-chart", Order = 5, IsActive = true },

                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), ModuleKey = "CUSTOMERS", Name = "Directorio de Clientes", Route = "/customers", Icon = "bi-people", Order = 1, IsActive = true },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333334"), ModuleKey = "CUSTOMERS", Name = "Nuevo Cliente / Proveedor", Route = "/customers/new", Icon = "bi-person-plus", Order = 2, IsActive = true },

                new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), ModuleKey = "CASH", Name = "Arqueo y Movimientos", Route = "/cash", Icon = "bi-cash-stack", Order = 1, IsActive = true },
                new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444445"), ModuleKey = "CASH", Name = "Consolidado de Caja", Route = "/cash/consolidated", Icon = "bi-calendar3-range", Order = 2, IsActive = true },

                new() { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), ModuleKey = "INVOICING", Name = "Facturas Emitidas", Route = "/invoices", Icon = "bi-receipt", Order = 1, IsActive = true },
                new() { Id = Guid.Parse("55555555-5555-5555-5555-555555555556"), ModuleKey = "INVOICING", Name = "Nueva Factura", Route = "/invoices/new", Icon = "bi-plus-circle", Order = 2, IsActive = true },

                new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666661"), ModuleKey = "ADMIN", Name = "Usuarios del Sistema", Route = "/admin/users", Icon = "bi-people-fill", Order = 1, IsActive = true },
                new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666662"), ModuleKey = "ADMIN", Name = "Configuración de Módulos", Route = "/admin/modules", Icon = "bi-grid-fill", Order = 2, IsActive = true },
                new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666663"), ModuleKey = "ADMIN", Name = "Configuración de Menús", Route = "/admin/menus", Icon = "bi-list-ul", Order = 3, IsActive = true },
                new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666664"), ModuleKey = "ADMIN", Name = "Roles de Seguridad", Route = "/admin/roles", Icon = "bi-person-badge", Order = 4, IsActive = true },
                new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666665"), ModuleKey = "ADMIN", Name = "Matriz de Permisos", Route = "/admin/permissions", Icon = "bi-shield-check", Order = 5, IsActive = true },
                new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), ModuleKey = "ADMIN", Name = "Mis Passkeys", Route = "/auth/passkeys", Icon = "bi-fingerprint", Order = 6, IsActive = true },
                new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666667"), ModuleKey = "ADMIN", Name = "Autenticador Móvil (MFA)", Route = "/auth/mfa", Icon = "bi-shield-lock-fill", Order = 7, IsActive = true }
            };

            _dbContext.AppModules.AddRange(defaultModules);
            _dbContext.MenuItems.AddRange(defaultMenus);
            await _dbContext.SaveChangesAsync(ct);
        }
        else
        {
            if (!await _dbContext.MenuItems.AnyAsync(m => m.Route == "/profile", ct))
            {
                _dbContext.MenuItems.Add(new()
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666660"),
                    ModuleKey = "ADMIN",
                    Name = "Mi Perfil",
                    Route = "/profile",
                    Icon = "bi-person-circle",
                    Order = 0,
                    IsActive = true
                });
                await _dbContext.SaveChangesAsync(ct);
            }

            if (!await _dbContext.MenuItems.AnyAsync(m => m.Route == "/auth/mfa", ct))
            {
                _dbContext.MenuItems.Add(new()
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666667"),
                    ModuleKey = "ADMIN",
                    Name = "Autenticador Móvil (MFA)",
                    Route = "/auth/mfa",
                    Icon = "bi-shield-lock-fill",
                    Order = 7,
                    IsActive = true
                });
                await _dbContext.SaveChangesAsync(ct);
            }

            if (!await _dbContext.MenuItems.AnyAsync(m => m.Route == "/inventory/audit-activity", ct))
            {
                _dbContext.MenuItems.Add(new()
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222228"),
                    ModuleKey = "INVENTORY",
                    Name = "Auditoría de Actividad",
                    Route = "/inventory/audit-activity",
                    Icon = "bi-graph-up-arrow",
                    Order = 6,
                    IsActive = true
                });
                await _dbContext.SaveChangesAsync(ct);
            }

            if (!await _dbContext.MenuItems.AnyAsync(m => m.Route == "/audit/productivity", ct))
            {
                _dbContext.MenuItems.Add(new()
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666668"),
                    ModuleKey = "ADMIN",
                    Name = "Productividad y Auditoría",
                    Route = "/audit/productivity",
                    Icon = "bi-speedometer2",
                    Order = 8,
                    IsActive = true
                });
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        // 2. Ensure default roles exist including 'Admin'
        string[] roles = ["Admin", "Administrador", "Supervisor", "Tasador", "Cajero", "Vendedor"];
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 3. Ensure User 'walvarez@ingnova.team' exists with role 'Admin' and password '#Wa3215252.'
        const string adminEmail = "walvarez@ingnova.team";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail)
            ?? await _userManager.FindByNameAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var createRes = await _userManager.CreateAsync(adminUser, "#Wa3215252.");
            if (createRes.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                await _userManager.AddToRoleAsync(adminUser, "Administrador");
            }
        }
        else
        {
            if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }
            if (!await _userManager.IsInRoleAsync(adminUser, "Administrador"))
            {
                await _userManager.AddToRoleAsync(adminUser, "Administrador");
            }
        }

        // 4. Ensure Role 'Admin' and 'Administrador' have ALL permissions granted on all menus
        var allMenus = await _dbContext.MenuItems.ToListAsync(ct);
        foreach (var menu in allMenus)
        {
            foreach (var roleName in new[] { "Admin", "Administrador" })
            {
                var perm = await _dbContext.RolePermissions
                    .FirstOrDefaultAsync(p => p.RoleName == roleName && p.MenuRoute == menu.Route, ct);

                if (perm == null)
                {
                    _dbContext.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleName = roleName,
                        ModuleKey = menu.ModuleKey,
                        MenuRoute = menu.Route,
                        CanView = true,
                        CanCreate = true,
                        CanEdit = true,
                        CanDelete = true,
                        CanAdmin = true
                    });
                }
                else
                {
                    perm.CanView = true;
                    perm.CanCreate = true;
                    perm.CanEdit = true;
                    perm.CanDelete = true;
                    perm.CanAdmin = true;
                }
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        _seededTenants[dbName] = true;
        }
        finally
        {
            _seedLock.Release();
        }
    }

    public Task<Result<IReadOnlyList<AppModuleDto>>> Handle(GetNavigationModulesQuery request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
        {
            await EnsureNavigationSeededAsync(cancellationToken);

            var modules = await _dbContext.AppModules
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Order)
                .Select(m => new AppModuleDto(m.Id, m.Key, m.Name, m.Icon, m.Description, m.Order, m.IsActive))
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                var user = await _userManager.FindByIdAsync(request.UserId)
                    ?? await _userManager.FindByNameAsync(request.UserId)
                    ?? await _userManager.FindByEmailAsync(request.UserId);

                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    bool isAdmin = roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrador", StringComparison.OrdinalIgnoreCase));
                    if (!isAdmin)
                    {
                        var allowedRoutes = await _dbContext.RolePermissions
                            .AsNoTracking()
                            .Where(p => roles.Contains(p.RoleName) && p.CanView)
                            .Select(p => p.MenuRoute)
                            .ToListAsync(cancellationToken);

                        var allowedModuleKeys = await _dbContext.MenuItems
                            .AsNoTracking()
                            .Where(m => m.Route == "/profile" || m.Route == "/audit/productivity" || m.Route == "/inventory/audit-activity" || allowedRoutes.Contains(m.Route))
                            .Select(m => m.ModuleKey)
                            .Distinct()
                            .ToListAsync(cancellationToken);

                        modules = modules.Where(m => allowedModuleKeys.Contains(m.Key, StringComparer.OrdinalIgnoreCase)).ToList();
                    }
                }
            }

            return Result.Success<IReadOnlyList<AppModuleDto>>(modules);
        });

    public Task<Result<IReadOnlyList<MenuDto>>> Handle(GetNavigationMenusQuery request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
        {
            await EnsureNavigationSeededAsync(cancellationToken);

            var query = _dbContext.MenuItems.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.ModuleKey) && !request.ModuleKey.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.ModuleKey == request.ModuleKey);
            }

            var allItems = await query
                .OrderBy(m => m.Order)
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    ModuleKey = m.ModuleKey,
                    ParentId = m.ParentId,
                    Name = m.Name,
                    Route = m.Route,
                    Icon = m.Icon,
                    Order = m.Order,
                    IsActive = m.IsActive,
                    RequiredPermission = m.RequiredPermission
                })
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                var user = await _userManager.FindByIdAsync(request.UserId)
                    ?? await _userManager.FindByNameAsync(request.UserId)
                    ?? await _userManager.FindByEmailAsync(request.UserId);

                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    bool isAdmin = roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrador", StringComparison.OrdinalIgnoreCase));
                    if (!isAdmin)
                    {
                        var allowedRoutes = await _dbContext.RolePermissions
                            .AsNoTracking()
                            .Where(p => roles.Contains(p.RoleName) && p.CanView)
                            .Select(p => p.MenuRoute)
                            .ToListAsync(cancellationToken);

                        // Profile and productivity/audit menus are always visible to all roles
                        allItems = allItems.Where(m => m.Route == "/profile" || m.Route == "/audit/productivity" || m.Route == "/inventory/audit-activity" || allowedRoutes.Contains(m.Route, StringComparer.OrdinalIgnoreCase)).ToList();
                    }
                }
            }

            // Build hierarchy for root menus
            var rootMenus = allItems.Where(m => m.ParentId == null).ToList();
            foreach (var root in rootMenus)
            {
                root.Children = allItems.Where(m => m.ParentId == root.Id).OrderBy(m => m.Order).ToList();
            }

            return Result.Success<IReadOnlyList<MenuDto>>(rootMenus);
        });

    public Task<Result<IReadOnlyList<MenuDto>>> Handle(GetUserFavoritesQuery request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
        {
            await EnsureNavigationSeededAsync(cancellationToken);

            var pref = await _dbContext.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);
            var favoriteIds = new List<Guid>();
            if (pref != null && !string.IsNullOrWhiteSpace(pref.FavoriteMenuIdsJson))
            {
                try
                {
                    favoriteIds = JsonSerializer.Deserialize<List<Guid>>(pref.FavoriteMenuIdsJson) ?? [];
                }
                catch
                {
                    favoriteIds = [];
                }
            }

            // If user has no custom favorites set yet, provide default shortcuts
            if (favoriteIds.Count == 0)
            {
                var defaultFavs = await _dbContext.MenuItems
                    .AsNoTracking()
                    .Where(m => m.IsActive && (m.Route == "/contracts" || m.Route == "/inventory" || m.Route == "/cash"))
                    .Select(m => new MenuDto
                    {
                        Id = m.Id,
                        ModuleKey = m.ModuleKey,
                        Name = m.Name,
                        Route = m.Route,
                        Icon = m.Icon,
                        Order = m.Order,
                        IsActive = m.IsActive
                    })
                    .ToListAsync(cancellationToken);
                return Result.Success<IReadOnlyList<MenuDto>>(defaultFavs);
            }

            var favorites = await _dbContext.MenuItems
                .AsNoTracking()
                .Where(m => favoriteIds.Contains(m.Id) && m.IsActive)
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    ModuleKey = m.ModuleKey,
                    Name = m.Name,
                    Route = m.Route,
                    Icon = m.Icon,
                    Order = m.Order,
                    IsActive = m.IsActive
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IReadOnlyList<MenuDto>>(favorites);
        });

    public Task<Result<bool>> Handle(ToggleUserFavoriteCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var pref = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);
        var favoriteIds = new List<Guid>();

        if (pref == null)
        {
            pref = new UserMenuPreference
            {
                UserId = request.UserId,
                FavoriteMenuIdsJson = "[]",
                ModuleOrderKeysJson = "[]",
                UpdatedAtUtc = DateTime.UtcNow
            };
            _dbContext.UserPreferences.Add(pref);
        }
        else
        {
            try
            {
                favoriteIds = JsonSerializer.Deserialize<List<Guid>>(pref.FavoriteMenuIdsJson) ?? [];
            }
            catch
            {
                favoriteIds = [];
            }
        }

        if (favoriteIds.Contains(request.MenuId))
        {
            favoriteIds.Remove(request.MenuId);
        }
        else
        {
            favoriteIds.Add(request.MenuId);
        }

        pref.FavoriteMenuIdsJson = JsonSerializer.Serialize(favoriteIds);
        pref.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    });

    public Task<Result<Guid>> Handle(CreateModuleCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var module = new AppModule
        {
            Id = Guid.NewGuid(),
            Key = request.Key.ToUpperInvariant().Trim(),
            Name = request.Name.Trim(),
            Icon = request.Icon.Trim(),
            Description = request.Description.Trim(),
            Order = request.Order,
            IsActive = true
        };

        _dbContext.AppModules.Add(module);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(module.Id);
    });

    public Task<Result<bool>> Handle(UpdateModuleCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var module = await _dbContext.AppModules.FindAsync([request.Id], cancellationToken);
        if (module == null) return Result.Failure<bool>(Error.NotFound("Module.NotFound", "Módulo no encontrado."));

        module.Key = request.Key.ToUpperInvariant().Trim();
        module.Name = request.Name.Trim();
        module.Icon = request.Icon.Trim();
        module.Description = request.Description.Trim();
        module.Order = request.Order;
        module.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    });

    public Task<Result<bool>> Handle(ToggleModuleStatusCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var module = await _dbContext.AppModules.FindAsync([request.Id], cancellationToken);
        if (module == null) return Result.Failure<bool>(Error.NotFound("Module.NotFound", "Módulo no encontrado."));

        module.IsActive = !module.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    });

    public Task<Result<Guid>> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var menu = new MenuItem
        {
            Id = Guid.NewGuid(),
            ModuleKey = request.ModuleKey.ToUpperInvariant().Trim(),
            Name = request.Name.Trim(),
            Route = request.Route.Trim(),
            Icon = request.Icon.Trim(),
            Order = request.Order,
            ParentId = request.ParentId,
            IsActive = true
        };

        _dbContext.MenuItems.Add(menu);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(menu.Id);
    });

    public Task<Result<bool>> Handle(UpdateMenuCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var menu = await _dbContext.MenuItems.FindAsync([request.Id], cancellationToken);
        if (menu == null) return Result.Failure<bool>(Error.NotFound("Menu.NotFound", "Menú no encontrado."));

        menu.ModuleKey = request.ModuleKey.ToUpperInvariant().Trim();
        menu.Name = request.Name.Trim();
        menu.Route = request.Route.Trim();
        menu.Icon = request.Icon.Trim();
        menu.Order = request.Order;
        menu.ParentId = request.ParentId;
        menu.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    });

    public Task<Result<bool>> Handle(ToggleMenuStatusCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var menu = await _dbContext.MenuItems.FindAsync([request.Id], cancellationToken);
        if (menu == null) return Result.Failure<bool>(Error.NotFound("Menu.NotFound", "Menú no encontrado."));

        menu.IsActive = !menu.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    });

    public Task<Result<IReadOnlyList<UserItemDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var passkeyCounts = await _dbContext.PasskeyCredentials
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count, cancellationToken);

        var result = new List<UserItemDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            passkeyCounts.TryGetValue(user.Id, out var count);
            result.Add(new UserItemDto(
                user.Id,
                user.UserName ?? "",
                user.Email ?? "",
                user.PhoneNumber ?? "",
                roles.ToList(),
                user.IsActive,
                user.TwoFactorEnabled,
                count,
                user.CreatedAt,
                user.LastLoginAt));
        }

        return Result.Success<IReadOnlyList<UserItemDto>>(result);
    });

    public Task<Result<string>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<string>(Error.Validation("User.CreateFailed", msg));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!await _roleManager.RoleExistsAsync(request.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(request.Role));
            }
            await _userManager.AddToRoleAsync(user, request.Role);
        }

        return Result.Success(user.Id);
    });

    public Task<Result<bool>> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null) return Result.Failure<bool>(Error.NotFound("User.NotFound", "Usuario no encontrado."));

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        return Result.Success(true);
    });

    public Task<Result<bool>> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null) return Result.Failure<bool>(Error.NotFound("User.NotFound", "Usuario no encontrado."));

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(request.Role));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, request.Role);

        return Result.Success(true);
    });

    public Task<Result<IReadOnlyList<RoleItemDto>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var roles = await _roleManager.Roles.ToListAsync(cancellationToken);
        var result = new List<RoleItemDto>();

        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? "");
            result.Add(new RoleItemDto(
                role.Id,
                role.Name ?? "",
                GetRoleDescription(role.Name ?? ""),
                usersInRole.Count));
        }

        return Result.Success<IReadOnlyList<RoleItemDto>>(result);
    });

    private static string GetRoleDescription(string roleName) => roleName switch
    {
        "Administrador" => "Control total del sistema, seguridad, auditoría y configuraciones",
        "Supervisor" => "Autorización de contratos, arqueo mayor y descuentos",
        "Tasador" => "Valuación técnica de joyas y generación de contratos",
        "Cajero" => "Cobro de abonos, control de caja y liquidaciones",
        "Vendedor" => "Venta de vitrina e inventario comercial",
        _ => "Rol de operación general del sistema"
    };

    public Task<Result<string>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        if (await _roleManager.RoleExistsAsync(request.Name))
        {
            return Result.Failure<string>(Error.Conflict("Role.Exists", "El rol ya existe."));
        }

        var role = new IdentityRole(request.Name.Trim());
        var res = await _roleManager.CreateAsync(role);
        if (!res.Succeeded)
        {
            return Result.Failure<string>(Error.Validation("Role.CreateFailed", string.Join("; ", res.Errors.Select(e => e.Description))));
        }

        return Result.Success(role.Id);
    });

    public Task<Result<IReadOnlyList<PermissionItemDto>>> Handle(GetPermissionsByRoleQuery request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        await EnsureNavigationSeededAsync(cancellationToken);

        var existing = await _dbContext.RolePermissions
            .Where(p => p.RoleName == request.RoleName)
            .ToListAsync(cancellationToken);

        var menus = await _dbContext.MenuItems
            .Where(m => m.IsActive)
            .ToListAsync(cancellationToken);

        var list = new List<PermissionItemDto>();
        foreach (var menu in menus)
        {
            var perm = existing.FirstOrDefault(p => p.MenuRoute == menu.Route);
            if (perm != null)
            {
                list.Add(new PermissionItemDto(
                    perm.Id,
                    perm.RoleName,
                    perm.ModuleKey,
                    perm.MenuRoute,
                    perm.CanView,
                    perm.CanCreate,
                    perm.CanEdit,
                    perm.CanDelete,
                    perm.CanAdmin));
            }
            else
            {
                bool isAdmin = request.RoleName.Equals("Administrador", StringComparison.OrdinalIgnoreCase);
                list.Add(new PermissionItemDto(
                    Guid.NewGuid(),
                    request.RoleName,
                    menu.ModuleKey,
                    menu.Route,
                    CanView: true,
                    CanCreate: isAdmin,
                    CanEdit: isAdmin,
                    CanDelete: isAdmin,
                    CanAdmin: isAdmin));
            }
        }

        return Result.Success<IReadOnlyList<PermissionItemDto>>(list);
    });

    public Task<Result<bool>> Handle(SavePermissionsCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var existing = await _dbContext.RolePermissions
            .Where(p => p.RoleName == request.RoleName)
            .ToListAsync(cancellationToken);

        _dbContext.RolePermissions.RemoveRange(existing);

        var toAdd = request.Permissions.Select(p => new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleName = request.RoleName,
            ModuleKey = p.ModuleKey,
            MenuRoute = p.MenuRoute,
            CanView = p.CanView,
            CanCreate = p.CanCreate,
            CanEdit = p.CanEdit,
            CanDelete = p.CanDelete,
            CanAdmin = p.CanAdmin
        });

        _dbContext.RolePermissions.AddRange(toAdd);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    });

    public Task<Result<IReadOnlyList<PasskeyItemDto>>> Handle(GetPasskeysByUserQuery request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var user = await _userManager.FindByIdAsync(request.UserId)
            ?? await _userManager.FindByEmailAsync(request.UserId)
            ?? await _userManager.FindByNameAsync(request.UserId);

        var targetId = user?.Id ?? request.UserId;

        var keys = await _dbContext.PasskeyCredentials
            .Where(k => k.UserId == targetId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new PasskeyItemDto(k.Id, k.Name, k.CreatedAt, k.LastUsedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PasskeyItemDto>>(keys);
    });

    public Task<Result<bool>> Handle(DeletePasskeyCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(async () =>
    {
        var key = await _dbContext.PasskeyCredentials.FindAsync([request.Id], cancellationToken);
        if (key == null) return Result.Failure<bool>(Error.NotFound("Passkey.NotFound", "Passkey no encontrada."));

        _dbContext.PasskeyCredentials.Remove(key);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    });
}
