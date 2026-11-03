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

                // 3. Seed navigation modules and security
                if (tenant.Id == "pomona")
                {
                    try
                    {
                        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                        await sender.Send(new GetNavigationModulesQuery());
                        await SeedPomonaUsersAsync(tenantDb, logger);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed seeding initial Pomona navigation.");
                    }
                }
                else if (tenant.Id == "oro-plata")
                {
                    await SeedOroPlataTenantAsync(tenantDb, registry, logger);
                }

                // 4. Asegurar que los artículos iniciales de inventario queden desactivados una única vez para auditoría física
                await DeactivateInitialInventoryOnceAsync(tenantDb, tenant.Id, logger);

                logger.LogInformation("Tenant '{TenantId}' database initialized successfully.", tenant.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not initialize database for tenant '{TenantId}'. Continuing startup.", tenant.Id);
            }
        }
    }

    private static async Task EnsureDatabaseCreatedAsync(string targetConnectionString, string baseConnectionString, ILogger logger)
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
            catch (NpgsqlException ex) when (ex.SqlState == "3D000" || ex.Message.Contains("does not exist"))
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
                "SELECT 1 FROM pg_database WHERE datname = @dbName;", baseConn);
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
            logger.LogWarning(ex, "Note on database creation check: {Message}", ex.Message);
        }
    }

    private static async Task SeedOroPlataTenantAsync(JewerlyDbContext oroPlataDb, ITenantRegistry registry, ILogger logger)
    {
        try
        {
            // 1. Ensure AppModules exist
            if (!await oroPlataDb.Set<AppModule>().AnyAsync())
            {
                oroPlataDb.Set<AppModule>().AddRange(
                    new AppModule { Id = Guid.NewGuid(), Key = "CONTRACTS", Name = "Contratos", Icon = "bi-file-earmark-text", Description = "Gestión de contratos de compraventa y empeño", Order = 1, IsActive = true },
                    new AppModule { Id = Guid.NewGuid(), Key = "INVENTORY", Name = "Inventario", Icon = "bi-box-seam", Description = "Control de stock de joyas y relojería", Order = 2, IsActive = true },
                    new AppModule { Id = Guid.NewGuid(), Key = "CUSTOMERS", Name = "Clientes", Icon = "bi-people", Description = "Directorio y clientes", Order = 3, IsActive = true },
                    new AppModule { Id = Guid.NewGuid(), Key = "CASH", Name = "Caja", Icon = "bi-cash-stack", Description = "Arqueo y control diario de caja", Order = 4, IsActive = true },
                    new AppModule { Id = Guid.NewGuid(), Key = "INVOICING", Name = "Facturación", Icon = "bi-receipt", Description = "Emisión y control de facturas", Order = 5, IsActive = true },
                    new AppModule { Id = Guid.NewGuid(), Key = "ADMIN", Name = "Seguridad", Icon = "bi-shield-lock", Description = "Usuarios, módulos, menús, roles y permisos", Order = 6, IsActive = true }
                );
                await oroPlataDb.SaveChangesAsync();
            }

            // 2. Ensure MenuItems exist
            if (!await oroPlataDb.Set<MenuItem>().AnyAsync())
            {
                oroPlataDb.Set<MenuItem>().AddRange(
                    new MenuItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111112"), ModuleKey = "CONTRACTS", Name = "Lista de Contratos", Route = "/contracts", Icon = "bi-card-list", Order = 1, IsActive = true },
                    new MenuItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111113"), ModuleKey = "CONTRACTS", Name = "Nuevo Contrato", Route = "/contracts/new", Icon = "bi-plus-circle", Order = 2, IsActive = true },
                    new MenuItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222223"), ModuleKey = "INVENTORY", Name = "Stock General", Route = "/inventory", Icon = "bi-box-seam", Order = 1, IsActive = true },
                    new MenuItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222224"), ModuleKey = "INVENTORY", Name = "Nueva Joya", Route = "/inventory/jewels/new", Icon = "bi-gem", Order = 2, IsActive = true },
                    new MenuItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222225"), ModuleKey = "INVENTORY", Name = "Nuevo Reloj", Route = "/inventory/watches/new", Icon = "bi-watch", Order = 3, IsActive = true },
                    new MenuItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222226"), ModuleKey = "INVENTORY", Name = "Descargos", Route = "/inventory/discharge", Icon = "bi-arrow-down-left-square", Order = 4, IsActive = true },
                    new MenuItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222227"), ModuleKey = "INVENTORY", Name = "Consolidado Stock", Route = "/inventory/consolidated", Icon = "bi-pie-chart", Order = 5, IsActive = true },
                    new MenuItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333334"), ModuleKey = "CUSTOMERS", Name = "Directorio Clientes", Route = "/customers", Icon = "bi-person-lines-fill", Order = 1, IsActive = true },
                    new MenuItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333335"), ModuleKey = "CUSTOMERS", Name = "Nuevo Cliente", Route = "/customers/new", Icon = "bi-person-plus", Order = 2, IsActive = true },
                    new MenuItem { Id = Guid.Parse("44444444-4444-4444-4444-444444444445"), ModuleKey = "CASH", Name = "Movimiento Diario", Route = "/cash/daily", Icon = "bi-calendar-check", Order = 1, IsActive = true },
                    new MenuItem { Id = Guid.Parse("44444444-4444-4444-4444-444444444446"), ModuleKey = "CASH", Name = "Consolidado Histórico", Route = "/cash/consolidated", Icon = "bi-graph-up", Order = 2, IsActive = true },
                    new MenuItem { Id = Guid.Parse("55555555-5555-5555-5555-555555555556"), ModuleKey = "INVOICING", Name = "Lista Facturas", Route = "/invoices", Icon = "bi-receipt-cutoff", Order = 1, IsActive = true },
                    new MenuItem { Id = Guid.Parse("55555555-5555-5555-5555-555555555557"), ModuleKey = "INVOICING", Name = "Nueva Factura", Route = "/invoices/new", Icon = "bi-plus-circle", Order = 2, IsActive = true },
                    new MenuItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666667"), ModuleKey = "ADMIN", Name = "Usuarios", Route = "/admin/users", Icon = "bi-people-fill", Order = 1, IsActive = true },
                    new MenuItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666668"), ModuleKey = "ADMIN", Name = "Roles y Perfiles", Route = "/admin/roles", Icon = "bi-award-fill", Order = 2, IsActive = true },
                    new MenuItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666669"), ModuleKey = "ADMIN", Name = "Matriz de Permisos", Route = "/admin/permissions", Icon = "bi-key-fill", Order = 3, IsActive = true },
                    new MenuItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666670"), ModuleKey = "ADMIN", Name = "Módulos del Sistema", Route = "/admin/modules", Icon = "bi-grid-fill", Order = 4, IsActive = true },
                    new MenuItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666671"), ModuleKey = "ADMIN", Name = "Menús de Navegación", Route = "/admin/menus", Icon = "bi-menu-button-wide-fill", Order = 5, IsActive = true },
                    new MenuItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666660"), ModuleKey = "ADMIN", Name = "Mi Perfil", Route = "/profile", Icon = "bi-person-circle", Order = 0, IsActive = true },
                    new MenuItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666672"), ModuleKey = "ADMIN", Name = "Seguridad & Passkeys", Route = "/auth/passkeys", Icon = "bi-shield-lock-fill", Order = 6, IsActive = true },
                    new MenuItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666673"), ModuleKey = "ADMIN", Name = "Autenticador Móvil (MFA)", Route = "/auth/mfa", Icon = "bi-phone", Order = 7, IsActive = true }
                );
                await oroPlataDb.SaveChangesAsync();
            }

            // 3. Ensure Roles
            var adminRoleId = "9f51d7c5-d662-443d-932b-38e0bc1076a5";
            var adminRole2Id = "43b031c0-aedd-4c6c-9bf3-5e857b09534b";

            var roles = await oroPlataDb.Roles.ToListAsync();
            if (!roles.Any(r => r.NormalizedName == "ADMINISTRADOR"))
            {
                oroPlataDb.Roles.Add(new IdentityRole
                {
                    Id = adminRoleId,
                    Name = "Administrador",
                    NormalizedName = "ADMINISTRADOR",
                    ConcurrencyStamp = adminRoleId
                });
            }
            if (!roles.Any(r => r.NormalizedName == "ADMIN"))
            {
                oroPlataDb.Roles.Add(new IdentityRole
                {
                    Id = adminRole2Id,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = adminRole2Id
                });
            }
            string[] otherRoles = ["Supervisor", "Tasador", "Cajero", "Vendedor"];
            foreach (var rName in otherRoles)
            {
                if (!roles.Any(r => r.NormalizedName == rName.ToUpperInvariant()))
                {
                    var newId = Guid.NewGuid().ToString();
                    oroPlataDb.Roles.Add(new IdentityRole { Id = newId, Name = rName, NormalizedName = rName.ToUpperInvariant(), ConcurrencyStamp = newId });
                }
            }
            await oroPlataDb.SaveChangesAsync();

            // 4. Ensure User 'walvarez@ingnova.team' exists
            var user = await oroPlataDb.Users.FirstOrDefaultAsync(u => u.Email == "walvarez@ingnova.team");
            if (user == null)
            {
                var pomonaCs = registry.GetConnectionString("pomona");
                var pomonaOptions = new DbContextOptionsBuilder<JewerlyDbContext>().UseNpgsql(pomonaCs).Options;
                await using var pomonaDb = new JewerlyDbContext(pomonaOptions);

                var pomonaUser = await pomonaDb.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "walvarez@ingnova.team");
                if (pomonaUser != null)
                {
                    user = new ApplicationUser
                    {
                        Id = pomonaUser.Id,
                        UserName = pomonaUser.UserName,
                        NormalizedUserName = pomonaUser.NormalizedUserName,
                        Email = pomonaUser.Email,
                        NormalizedEmail = pomonaUser.NormalizedEmail,
                        EmailConfirmed = pomonaUser.EmailConfirmed,
                        PasswordHash = pomonaUser.PasswordHash,
                        SecurityStamp = pomonaUser.SecurityStamp,
                        ConcurrencyStamp = pomonaUser.ConcurrencyStamp,
                        PhoneNumber = pomonaUser.PhoneNumber,
                        PhoneNumberConfirmed = pomonaUser.PhoneNumberConfirmed,
                        TwoFactorEnabled = pomonaUser.TwoFactorEnabled,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    oroPlataDb.Users.Add(user);
                    await oroPlataDb.SaveChangesAsync();

                    // Assign Roles
                    if (!await oroPlataDb.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRoleId))
                    {
                        oroPlataDb.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = adminRoleId });
                    }
                    if (!await oroPlataDb.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole2Id))
                    {
                        oroPlataDb.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = adminRole2Id });
                    }
                    await oroPlataDb.SaveChangesAsync();

                    // Copy Passkeys if any
                    var pomonaPasskeys = await pomonaDb.PasskeyCredentials.AsNoTracking().Where(p => p.UserId == pomonaUser.Id).ToListAsync();
                    foreach (var pk in pomonaPasskeys)
                    {
                        if (!await oroPlataDb.PasskeyCredentials.AnyAsync(p => p.Id == pk.Id))
                        {
                            oroPlataDb.PasskeyCredentials.Add(new PasskeyCredential(
                                pk.UserId, pk.Name, pk.CredentialId, pk.PublicKey, pk.UserHandle, pk.SignatureCounter, pk.AaGuid
                            ) { Id = pk.Id, CreatedAt = pk.CreatedAt, LastUsedAt = pk.LastUsedAt });
                        }
                    }
                    await oroPlataDb.SaveChangesAsync();
                }
            }

            // 5. Ensure RolePermissions for Admin & Administrador across all menus
            var allMenus = await oroPlataDb.Set<MenuItem>().ToListAsync();
            foreach (var menu in allMenus)
            {
                foreach (var roleName in new[] { "Admin", "Administrador" })
                {
                    var perm = await oroPlataDb.Set<RolePermission>()
                        .FirstOrDefaultAsync(p => p.RoleName == roleName && p.MenuRoute == menu.Route);

                    if (perm == null)
                    {
                        oroPlataDb.Set<RolePermission>().Add(new RolePermission
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
                }
            }
            await oroPlataDb.SaveChangesAsync();

            // 6. Ensure Identification Types
            if (!await oroPlataDb.IdentificationTypes.AnyAsync())
            {
                oroPlataDb.IdentificationTypes.AddRange(
                    new Modules.Customers.Domain.IdentificationType(1, "Cédula de Ciudadanía"),
                    new Modules.Customers.Domain.IdentificationType(2, "Cédula de Extranjería"),
                    new Modules.Customers.Domain.IdentificationType(3, "NIT"),
                    new Modules.Customers.Domain.IdentificationType(4, "Pasaporte")
                );
                await oroPlataDb.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Note on OroPlata tenant seeding: {Message}", ex.Message);
        }
    }

    private static async Task SeedPomonaUsersAsync(JewerlyDbContext pomonaDb, ILogger logger)
    {
        try
        {
            var vendedorRoleId = "90e87e25-6d98-4d3b-ab05-38ea72d7ccfa";
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

                // Assign Vendedor role
                if (!await pomonaDb.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == vendedorRoleId))
                {
                    pomonaDb.UserRoles.Add(new IdentityUserRole<string>
                    {
                        UserId = userId,
                        RoleId = vendedorRoleId
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
}
