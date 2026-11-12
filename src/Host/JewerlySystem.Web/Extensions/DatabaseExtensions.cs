using JewerlySystem.Common.Application;
using JewerlySystem.Common.Infrastructure.Tenant;
using JewerlySystem.Modules.Auth.Application;
using JewerlySystem.Modules.Auth.Domain;
using JewerlySystem.Modules.Auth.Infrastructure;
using JewerlySystem.Web.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace JewerlySystem.Web.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JewerlyDbContext>>();
        var defaultTenant = registry.GetDefaultTenant();

        foreach (var tenant in registry.GetAllTenants())
        {
            try
            {
                logger.LogInformation("Initializing database for tenant '{TenantId}' ({TenantName})...", tenant.Id, tenant.Name);

                // 1. Ensure physical PostgreSQL database exists
                await EnsureDatabaseCreatedAsync(tenant.ConnectionString, defaultTenant.ConnectionString, logger);

                // 2. Ensure schema & tables exist
                var optionsBuilder = new DbContextOptionsBuilder<JewerlyDbContext>();
                optionsBuilder.UseNpgsql(tenant.ConnectionString);

                await using var tenantDb = new JewerlyDbContext(optionsBuilder.Options);
                await tenantDb.Database.EnsureCreatedAsync();
                await EnsureUserActivityLogsTableAsync(tenantDb, logger);
                await EnsureInventoryColumnsAsync(tenantDb, logger);
                await EnsurePersonColumnsAsync(tenantDb, logger);

                // 3. Ensure universal baseline (Modules, Menus, Roles, Permissions, Users, IdentificationTypes, Persons)
                await EnsureTenantBaselineAsync(tenantDb, tenant, registry, logger);

                // 4. Ensure DeactivateInitialInventoryOnceAsync
                await DeactivateInitialInventoryOnceAsync(tenantDb, tenant.Id, logger);

                logger.LogInformation("Tenant '{TenantId}' database initialized successfully.", tenant.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initializing database for tenant '{TenantId}'. Continuing startup.", tenant.Id);
            }
        }
    }

    public static async Task EnsureDatabaseCreatedAsync(string targetConnectionString, string baseConnectionString, ILogger logger)
    {
        try
        {
            // 1. Test direct connection to target DB
            try
            {
                await using var directConn = new NpgsqlConnection(targetConnectionString);
                await directConn.OpenAsync();
                return; // Target DB exists and is accessible
            }
            catch (NpgsqlException ex) when (ex.SqlState == "3D000" || ex.Message.Contains("does not exist") || ex.Message.Contains("no existe"))
            {
                logger.LogInformation("Target DB does not exist yet. Will create it from base database.");
            }

            // 2. Extract target database name
            var csBuilder = new NpgsqlConnectionStringBuilder(targetConnectionString);
            var targetDatabase = csBuilder.Database;

            if (string.IsNullOrWhiteSpace(targetDatabase)) return;

            // 3. Connect to the base database (which is guaranteed to exist and have superuser/owner privileges)
            await using var baseConn = new NpgsqlConnection(baseConnectionString);
            await baseConn.OpenAsync();

            await using var checkCmd = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE LOWER(datname) = LOWER(@dbName);", baseConn);
            checkCmd.Parameters.AddWithValue("@dbName", targetDatabase);

            var exists = await checkCmd.ExecuteScalarAsync();
            if (exists == null)
            {
                logger.LogInformation("Creating PostgreSQL database '{Database}'...", targetDatabase);
                await using var createCmd = new NpgsqlCommand(
                    $"CREATE DATABASE \"{targetDatabase}\";", baseConn);
                await createCmd.ExecuteNonQueryAsync();
                logger.LogInformation("PostgreSQL database '{Database}' created successfully.", targetDatabase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Note on database creation check: {Message}", ex.Message);
            throw;
        }
    }

    public static List<AppModule> GetCanonicalModules() =>
    [
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Key = "CONTRACTS", Name = "Contratos", Icon = "bi-file-earmark-text", Description = "Gestión de contratos de compraventa y empeño", Order = 1, IsActive = true },
        new() { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Key = "INVENTORY", Name = "Inventario", Icon = "bi-box-seam", Description = "Control de stock de joyas y relojería", Order = 2, IsActive = true },
        new() { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Key = "CUSTOMERS", Name = "Clientes", Icon = "bi-people", Description = "Directorio y clientes", Order = 3, IsActive = true },
        new() { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Key = "CASH", Name = "Caja", Icon = "bi-cash-stack", Description = "Arqueo y control diario de caja", Order = 4, IsActive = true },
        new() { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), Key = "INVOICING", Name = "Facturación", Icon = "bi-receipt", Description = "Emisión y control de facturas", Order = 5, IsActive = true },
        new() { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Key = "ADMIN", Name = "Seguridad", Icon = "bi-shield-lock", Description = "Usuarios, módulos, menús, roles y permisos", Order = 6, IsActive = true }
    ];

    public static List<MenuItem> GetCanonicalMenus() =>
    [
        new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), ModuleKey = "CONTRACTS", Name = "Todos los Contratos", Route = "/contracts", Icon = "bi-file-earmark-text", Order = 1, IsActive = true },
        new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111112"), ModuleKey = "CONTRACTS", Name = "Nuevo Contrato", Route = "/contracts/new", Icon = "bi-plus-circle", Order = 2, IsActive = true },
        new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111113"), ModuleKey = "CONTRACTS", Name = "Calculadora Ley Oro", Route = "/utilities/gold-calculator", Icon = "bi-calculator", Order = 3, IsActive = true },

        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), ModuleKey = "INVENTORY", Name = "Catálogo de Joyas y Relojes", Route = "/inventory", Icon = "bi-box-seam", Order = 1, IsActive = true },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222223"), ModuleKey = "INVENTORY", Name = "Nueva Joya", Route = "/inventory/jewels/new", Icon = "bi-gem", Order = 2, IsActive = true },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222224"), ModuleKey = "INVENTORY", Name = "Nuevo Reloj", Route = "/inventory/watches/new", Icon = "bi-watch", Order = 3, IsActive = true },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222225"), ModuleKey = "INVENTORY", Name = "Descargar Artículo", Route = "/inventory/discharge", Icon = "bi-box-arrow-right", Order = 4, IsActive = true },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222226"), ModuleKey = "INVENTORY", Name = "Consolidado de Inventario", Route = "/inventory/consolidated", Icon = "bi-pie-chart", Order = 5, IsActive = true },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222228"), ModuleKey = "INVENTORY", Name = "Auditoría de Actividad", Route = "/inventory/audit-activity", Icon = "bi-graph-up-arrow", Order = 6, IsActive = true },

        new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), ModuleKey = "CUSTOMERS", Name = "Directorio de Clientes", Route = "/customers", Icon = "bi-people", Order = 1, IsActive = true },
        new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333334"), ModuleKey = "CUSTOMERS", Name = "Nuevo Cliente / Proveedor", Route = "/customers/new", Icon = "bi-person-plus", Order = 2, IsActive = true },

        new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), ModuleKey = "CASH", Name = "Arqueo y Movimientos", Route = "/cash", Icon = "bi-cash-stack", Order = 1, IsActive = true },
        new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444445"), ModuleKey = "CASH", Name = "Consolidado de Caja", Route = "/cash/consolidated", Icon = "bi-calendar3-range", Order = 2, IsActive = true },

        new() { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), ModuleKey = "INVOICING", Name = "Facturas Emitidas", Route = "/invoices", Icon = "bi-receipt", Order = 1, IsActive = true },
        new() { Id = Guid.Parse("55555555-5555-5555-5555-555555555556"), ModuleKey = "INVOICING", Name = "Nueva Factura", Route = "/invoices/new", Icon = "bi-plus-circle", Order = 2, IsActive = true },

        new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666660"), ModuleKey = "ADMIN", Name = "Mi Perfil", Route = "/profile", Icon = "bi-person-circle", Order = 0, IsActive = true },
        new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666661"), ModuleKey = "ADMIN", Name = "Usuarios del Sistema", Route = "/admin/users", Icon = "bi-people-fill", Order = 1, IsActive = true },
        new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666662"), ModuleKey = "ADMIN", Name = "Configuración de Módulos", Route = "/admin/modules", Icon = "bi-grid-fill", Order = 2, IsActive = true },
        new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666663"), ModuleKey = "ADMIN", Name = "Configuración de Menús", Route = "/admin/menus", Icon = "bi-list-ul", Order = 3, IsActive = true },
        new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666664"), ModuleKey = "ADMIN", Name = "Roles de Seguridad", Route = "/admin/roles", Icon = "bi-person-badge", Order = 4, IsActive = true },
        new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666665"), ModuleKey = "ADMIN", Name = "Matriz de Permisos", Route = "/admin/permissions", Icon = "bi-shield-check", Order = 5, IsActive = true },
        new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), ModuleKey = "ADMIN", Name = "Mis Passkeys", Route = "/auth/passkeys", Icon = "bi-fingerprint", Order = 6, IsActive = true },
        new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666667"), ModuleKey = "ADMIN", Name = "Autenticador Móvil (MFA)", Route = "/auth/mfa", Icon = "bi-shield-lock-fill", Order = 7, IsActive = true },
        new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666668"), ModuleKey = "ADMIN", Name = "Productividad y Auditoría", Route = "/audit/productivity", Icon = "bi-speedometer2", Order = 8, IsActive = true }
    ];

    public static async Task EnsureTenantBaselineAsync(JewerlyDbContext tenantDb, TenantInfo tenant, ITenantRegistry registry, ILogger logger)
    {
        // 0. Ensure schema migrations and columns exist
        await EnsureUserActivityLogsTableAsync(tenantDb, logger);
        await EnsureInventoryColumnsAsync(tenantDb, logger);
        await EnsurePersonColumnsAsync(tenantDb, logger);

        // 1. Ensure Modules
        var canonicalModules = GetCanonicalModules();
        var existingModules = await tenantDb.Set<AppModule>().ToListAsync();
        foreach (var mod in canonicalModules)
        {
            var existing = existingModules.FirstOrDefault(m => m.Id == mod.Id)
                        ?? existingModules.FirstOrDefault(m => string.Equals(m.Key, mod.Key, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                tenantDb.Set<AppModule>().Add(new AppModule
                {
                    Id = mod.Id,
                    Key = mod.Key,
                    Name = mod.Name,
                    Icon = mod.Icon,
                    Description = mod.Description,
                    Order = mod.Order,
                    IsActive = true
                });
            }
            else
            {
                existing.Key = mod.Key;
                existing.Name = mod.Name;
                existing.Icon = mod.Icon;
                existing.Description = mod.Description;
                existing.Order = mod.Order;
                existing.IsActive = true;
            }
        }
        await tenantDb.SaveChangesAsync();

        // 2. Ensure Menus
        var canonicalMenus = GetCanonicalMenus();
        var existingMenus = await tenantDb.Set<MenuItem>().ToListAsync();

        // Deactivate obsolete routes if any exist from older schemas
        foreach (var obs in existingMenus.Where(m => m.Route == "/cash/daily"))
        {
            obs.IsActive = false;
        }

        foreach (var menu in canonicalMenus)
        {
            var existing = existingMenus.FirstOrDefault(m => m.Id == menu.Id)
                        ?? existingMenus.FirstOrDefault(m => string.Equals(m.Route, menu.Route, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                tenantDb.Set<MenuItem>().Add(new MenuItem
                {
                    Id = menu.Id,
                    ModuleKey = menu.ModuleKey,
                    Name = menu.Name,
                    Route = menu.Route,
                    Icon = menu.Icon,
                    Order = menu.Order,
                    IsActive = true
                });
            }
            else
            {
                existing.Name = menu.Name;
                existing.Route = menu.Route;
                existing.Icon = menu.Icon;
                existing.ModuleKey = menu.ModuleKey;
                existing.Order = menu.Order;
                existing.IsActive = true;
            }
        }
        await tenantDb.SaveChangesAsync();

        // 3. Ensure Roles
        var roles = await tenantDb.Roles.ToListAsync();
        string[] requiredRoles = ["Admin", "Administrador", "Supervisor", "Tasador", "Cajero", "Vendedor"];
        foreach (var rName in requiredRoles)
        {
            if (!roles.Any(r => string.Equals(r.Name, rName, StringComparison.OrdinalIgnoreCase) || string.Equals(r.NormalizedName, rName.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase)))
            {
                var newId = Guid.NewGuid().ToString();
                tenantDb.Roles.Add(new IdentityRole
                {
                    Id = newId,
                    Name = rName,
                    NormalizedName = rName.ToUpperInvariant(),
                    ConcurrencyStamp = newId
                });
            }
        }
        await tenantDb.SaveChangesAsync();
        roles = await tenantDb.Roles.ToListAsync();

        // 4. Ensure Permissions across all roles
        var allMenus = await tenantDb.Set<MenuItem>().ToListAsync();
        var existingPerms = await tenantDb.Set<RolePermission>().ToListAsync();

        foreach (var menu in allMenus)
        {
            foreach (var role in roles)
            {
                var perm = existingPerms.FirstOrDefault(p =>
                    string.Equals(p.RoleName, role.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.MenuRoute, menu.Route, StringComparison.OrdinalIgnoreCase));

                bool canView = false;
                bool canCreate = false;
                bool canEdit = false;
                bool canDelete = false;
                bool canAdmin = false;

                if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(role.Name, "Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    canView = canCreate = canEdit = canDelete = canAdmin = true;
                }
                else if (string.Equals(role.Name, "Supervisor", StringComparison.OrdinalIgnoreCase))
                {
                    canView = true;
                    canCreate = canEdit = !menu.Route.StartsWith("/admin");
                }
                else if (string.Equals(role.Name, "Tasador", StringComparison.OrdinalIgnoreCase))
                {
                    canView = menu.ModuleKey == "CONTRACTS" || menu.ModuleKey == "INVENTORY" || menu.Route == "/profile";
                    canCreate = canEdit = canView;
                }
                else if (string.Equals(role.Name, "Cajero", StringComparison.OrdinalIgnoreCase))
                {
                    canView = menu.ModuleKey == "CASH" || menu.ModuleKey == "INVOICING" || menu.ModuleKey == "CONTRACTS" || menu.Route == "/profile";
                    canCreate = canEdit = canView;
                }
                else if (string.Equals(role.Name, "Vendedor", StringComparison.OrdinalIgnoreCase))
                {
                    canView = menu.ModuleKey == "INVOICING" || menu.ModuleKey == "CUSTOMERS" || menu.ModuleKey == "INVENTORY" || menu.Route == "/profile";
                    canCreate = canView;
                }

                if (canView)
                {
                    if (perm == null)
                    {
                        tenantDb.Set<RolePermission>().Add(new RolePermission
                        {
                            Id = Guid.NewGuid(),
                            RoleName = role.Name ?? string.Empty,
                            ModuleKey = menu.ModuleKey,
                            MenuRoute = menu.Route,
                            CanView = canView,
                            CanCreate = canCreate,
                            CanEdit = canEdit,
                            CanDelete = canDelete,
                            CanAdmin = canAdmin
                        });
                    }
                    else
                    {
                        perm.CanView = canView;
                        perm.CanCreate = canCreate;
                        perm.CanEdit = canEdit;
                        perm.CanDelete = canDelete;
                        perm.CanAdmin = canAdmin;
                    }
                }
            }
        }
        await tenantDb.SaveChangesAsync();

        // 5. Ensure System Admins in EVERY database
        await EnsureUniversalAdminsAsync(tenantDb, logger);

        // 6. Ensure Identification Types
        if (!await tenantDb.IdentificationTypes.AnyAsync())
        {
            tenantDb.IdentificationTypes.AddRange(
                new Modules.Customers.Domain.IdentificationType(1, "Cédula de Ciudadanía"),
                new Modules.Customers.Domain.IdentificationType(2, "Cédula de Extranjería"),
                new Modules.Customers.Domain.IdentificationType(3, "NIT"),
                new Modules.Customers.Domain.IdentificationType(4, "Pasaporte")
            );
            await tenantDb.SaveChangesAsync();
        }

        // 7. Tenant-specific user additions if applicable
        if (tenant.Id == "pomona")
        {
            await SeedPomonaSpecificUsersAsync(tenantDb, logger);
        }

        // 8. Backfill Person records
        await BackfillExistingUserPersonsAsync(tenantDb, logger);
    }

    private static async Task EnsureUniversalAdminsAsync(JewerlyDbContext tenantDb, ILogger logger)
    {
        var hasher = new PasswordHasher<ApplicationUser>();
        var adminRole = await tenantDb.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMINISTRADOR")
            ?? await tenantDb.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN");
        var admin2Role = await tenantDb.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN");

        var adminsToEnsure = new (string email, string pass, string phone, string first, string last1, string last2)[]
        {
            ("walvarez@ingnova.team", "#Wa3215252.", "3001234567", "William", "Alvarez", "Vargas"),
            ("walvarez@bezion.tech", "#Wa3215252.", "3001234567", "William", "Alvarez", "Vargas"),
            ("jmalvarezc@bezion.tech", "BeZ10N73ch", "3109876543", "Jose Manuel", "Alvarez", "Cotes"),
            ("admin@joyeriapomona.com", "Pomona2026*", "3007654321", "Administrador", "Pomona", "Sistema")
        };

        foreach (var adm in adminsToEnsure)
        {
            var user = await tenantDb.Users.FirstOrDefaultAsync(u => u.Email == adm.email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = adm.email,
                    NormalizedUserName = adm.email.ToUpperInvariant(),
                    Email = adm.email,
                    NormalizedEmail = adm.email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    PhoneNumber = adm.phone,
                    PhoneNumberConfirmed = true,
                    TwoFactorEnabled = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = hasher.HashPassword(user, adm.pass);
                tenantDb.Users.Add(user);
                await tenantDb.SaveChangesAsync();
                logger.LogInformation("Usuario admin {Email} aprovisionado en tenantDb.", adm.email);
            }
            else
            {
                user.EmailConfirmed = true;
                user.IsActive = true;
                await tenantDb.SaveChangesAsync();
            }

            // Assign Admin roles
            if (adminRole != null && !await tenantDb.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id))
            {
                tenantDb.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = adminRole.Id });
            }
            if (admin2Role != null && admin2Role.Id != adminRole?.Id && !await tenantDb.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == admin2Role.Id))
            {
                tenantDb.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = admin2Role.Id });
            }
            await tenantDb.SaveChangesAsync();

            // Ensure Person record
            var person = await tenantDb.Persons.FirstOrDefaultAsync(p => p.UserId == user.Id || p.Email == adm.email);
            if (person == null)
            {
                tenantDb.Persons.Add(new Modules.Customers.Domain.Person(
                    identificationTypeId: 1,
                    identificationNumber: "10" + Math.Abs(user.Id.GetHashCode()).ToString(),
                    fullName: $"{adm.first} {adm.last1} {adm.last2}".Trim(),
                    cellPhone: adm.phone,
                    email: adm.email,
                    personType: Modules.Customers.Domain.PersonType.Employee,
                    userId: user.Id,
                    firstName: adm.first,
                    firstLastName: adm.last1,
                    secondLastName: adm.last2,
                    address: "Sede Principal Joyería",
                    hireDate: DateTime.UtcNow
                ));
                await tenantDb.SaveChangesAsync();
            }
            else if (string.IsNullOrWhiteSpace(person.UserId))
            {
                person.UserId = user.Id;
                person.FirstName = adm.first;
                person.FirstLastName = adm.last1;
                person.SecondLastName = adm.last2;
                person.PersonType = Modules.Customers.Domain.PersonType.Employee;
                await tenantDb.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedPomonaSpecificUsersAsync(JewerlyDbContext pomonaDb, ILogger logger)
    {
        try
        {
            var email = "joglisjjc@gmail.com";
            var user = await pomonaDb.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                var userId = Guid.NewGuid().ToString();
                var hasher = new PasswordHasher<ApplicationUser>();
                var newUser = new ApplicationUser
                {
                    Id = userId,
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                newUser.PasswordHash = hasher.HashPassword(newUser, "JPomona123");
                pomonaDb.Users.Add(newUser);
                await pomonaDb.SaveChangesAsync();

                var vendedorRole = await pomonaDb.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "VENDEDOR");
                if (vendedorRole != null && !await pomonaDb.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == vendedorRole.Id))
                {
                    pomonaDb.UserRoles.Add(new IdentityUserRole<string>
                    {
                        UserId = userId,
                        RoleId = vendedorRole.Id
                    });
                    await pomonaDb.SaveChangesAsync();
                }
                logger.LogInformation("Usuario {Email} creado exitosamente con rol Vendedor en Pomona.", email);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error asegurando usuario Pomona: {Message}", ex.Message);
        }
    }

    private static async Task DeactivateInitialInventoryOnceAsync(JewerlyDbContext tenantDb, string tenantId, ILogger logger)
    {
        try
        {
            await tenantDb.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""__SystemMigrations"" (
                    ""MigrationId"" VARCHAR(150) PRIMARY KEY,
                    ""AppliedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL
                );");

            var migrationId = "20261103_DeactivateInitialInventoryForAudit";
            var alreadyApplied = await tenantDb.Database.SqlQueryRaw<string>(
                @"SELECT ""MigrationId"" FROM ""__SystemMigrations"" WHERE ""MigrationId"" = {0}", migrationId)
                .AnyAsync();

            if (!alreadyApplied)
            {
                logger.LogInformation("Aplicando migración de ajuste de inventario en tenant '{TenantId}'...", tenantId);
                var deactivatedCount = await tenantDb.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""Items"" SET ""Active"" = false WHERE ""Active"" = true;");

                await tenantDb.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO ""__SystemMigrations"" (""MigrationId"", ""AppliedAt"") VALUES ({0}, {1});",
                    migrationId, DateTime.UtcNow);

                logger.LogInformation("Auditoría de inventario inicializada: {Count} artículos desactivados en tenant '{TenantId}' (visibles en catálogo).", deactivatedCount, tenantId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo ejecutar DeactivateInitialInventoryOnceAsync en tenant '{TenantId}'.", tenantId);
        }
    }

    private static async Task EnsureUserActivityLogsTableAsync(JewerlyDbContext tenantDb, ILogger logger)
    {
        try
        {
            await tenantDb.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""UserActivityLogs"" (
                    ""Id"" BIGSERIAL PRIMARY KEY,
                    ""TenantId"" VARCHAR(50) NOT NULL,
                    ""UserId"" VARCHAR(450) NOT NULL,
                    ""UserName"" VARCHAR(256) NOT NULL,
                    ""ModuleKey"" VARCHAR(50) NOT NULL,
                    ""ActionKey"" VARCHAR(100) NOT NULL,
                    ""Category"" VARCHAR(100) NOT NULL,
                    ""Description"" VARCHAR(500) NOT NULL,
                    ""WeightScore"" INT NOT NULL,
                    ""EntityName"" VARCHAR(100) NOT NULL,
                    ""EntityReference"" VARCHAR(100),
                    ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_UserActivityLogs_TenantId_CreatedAt"" ON ""UserActivityLogs"" (""TenantId"", ""CreatedAt"" DESC);
                CREATE INDEX IF NOT EXISTS ""IX_UserActivityLogs_TenantId_UserId_CreatedAt"" ON ""UserActivityLogs"" (""TenantId"", ""UserId"", ""CreatedAt"" DESC);
                CREATE INDEX IF NOT EXISTS ""IX_UserActivityLogs_TenantId_ActionKey"" ON ""UserActivityLogs"" (""TenantId"", ""ActionKey"");
            ");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not ensure UserActivityLogs table exists: {Message}", ex.Message);
        }
    }

    private static async Task EnsureInventoryColumnsAsync(JewerlyDbContext tenantDb, ILogger logger)
    {
        try
        {
            await tenantDb.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""ImageUrl"" VARCHAR(1000);
                ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""ThumbnailUrl"" VARCHAR(1000);
                ALTER TABLE ""Watches"" ADD COLUMN IF NOT EXISTS ""Model"" VARCHAR(150);
            ");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Note on inventory columns migration: {Message}", ex.Message);
        }
    }

    private static async Task EnsurePersonColumnsAsync(JewerlyDbContext tenantDb, ILogger logger)
    {
        try
        {
            await tenantDb.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE ""Persons"" ADD COLUMN IF NOT EXISTS ""FirstName"" VARCHAR(100);
                ALTER TABLE ""Persons"" ADD COLUMN IF NOT EXISTS ""FirstLastName"" VARCHAR(100);
                ALTER TABLE ""Persons"" ADD COLUMN IF NOT EXISTS ""SecondLastName"" VARCHAR(100);
                ALTER TABLE ""Persons"" ADD COLUMN IF NOT EXISTS ""Address"" VARCHAR(250);
                ALTER TABLE ""Persons"" ADD COLUMN IF NOT EXISTS ""BirthDate"" TIMESTAMP WITHOUT TIME ZONE;
                ALTER TABLE ""Persons"" ADD COLUMN IF NOT EXISTS ""HireDate"" TIMESTAMP WITHOUT TIME ZONE;
                ALTER TABLE ""Persons"" ADD COLUMN IF NOT EXISTS ""UserId"" VARCHAR(450);
                UPDATE ""Persons"" SET ""FirstName"" = COALESCE(""FirstName"", ''), ""FirstLastName"" = COALESCE(""FirstLastName"", '') WHERE ""FirstName"" IS NULL OR ""FirstLastName"" IS NULL;
            ");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Note on Person columns migration: {Message}", ex.Message);
        }
    }

    private static async Task BackfillExistingUserPersonsAsync(JewerlyDbContext tenantDb, ILogger logger)
    {
        try
        {
            var users = await tenantDb.Users.ToListAsync();
            foreach (var user in users)
            {
                var person = await tenantDb.Persons.FirstOrDefaultAsync(p => p.UserId == user.Id || (user.Email != null && p.Email != null && p.Email.ToLower() == user.Email.ToLower()));
                if (person == null)
                {
                    var emailPrefix = (user.Email ?? user.UserName ?? "user").Split('@')[0];
                    var parts = emailPrefix.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
                    var fName = parts.Length > 0 ? char.ToUpper(parts[0][0]) + parts[0].Substring(1) : "Colaborador";
                    var lName = parts.Length > 1 ? char.ToUpper(parts[1][0]) + parts[1].Substring(1) : "Joyería";

                    var newPerson = new Modules.Customers.Domain.Person(
                        identificationTypeId: 1,
                        identificationNumber: $"EMP-{user.Id.Substring(0, Math.Min(8, user.Id.Length))}",
                        fullName: $"{fName} {lName}",
                        cellPhone: user.PhoneNumber ?? "",
                        email: user.Email ?? "",
                        personType: Modules.Customers.Domain.PersonType.Employee,
                        userId: user.Id,
                        firstName: fName,
                        firstLastName: lName,
                        hireDate: DateTime.UtcNow
                    );
                    tenantDb.Persons.Add(newPerson);
                }
                else
                {
                    bool changed = false;
                    if (string.IsNullOrWhiteSpace(person.UserId))
                    {
                        person.UserId = user.Id;
                        changed = true;
                    }
                    if (string.IsNullOrWhiteSpace(person.FirstName) && !string.IsNullOrWhiteSpace(person.FullName))
                    {
                        var parts = person.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        person.FirstName = parts.Length > 0 ? parts[0] : person.FullName;
                        person.FirstLastName = parts.Length > 1 ? parts[1] : "";
                        person.SecondLastName = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : null;
                        changed = true;
                    }
                    if (changed)
                    {
                        tenantDb.Persons.Update(person);
                    }
                }
            }
            await tenantDb.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Note on user person backfill: {Message}", ex.Message);
        }
    }
}
