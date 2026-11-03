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

        foreach (var tenant in registry.GetAllTenants())
        {
            try
            {
                logger.LogInformation("Initializing database for tenant '{TenantId}' ({TenantName})...", tenant.Id, tenant.Name);

                // 1. Ensure physical PostgreSQL database exists
                await EnsureDatabaseCreatedAsync(tenant.ConnectionString, logger);

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

                logger.LogInformation("Tenant '{TenantId}' database initialized successfully.", tenant.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not initialize database for tenant '{TenantId}'. Continuing startup.", tenant.Id);
            }
        }
    }

    private static async Task EnsureDatabaseCreatedAsync(string connectionString, ILogger logger)
    {
        try
        {
            var csBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            var targetDatabase = csBuilder.Database;

            if (string.IsNullOrWhiteSpace(targetDatabase)) return;

            // Connect to maintenance database 'postgres' to check if target DB exists
            var maintenanceBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = "postgres"
            };

            await using var conn = new NpgsqlConnection(maintenanceBuilder.ConnectionString);
            await conn.OpenAsync();

            await using var checkCmd = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @dbName;", conn);
            checkCmd.Parameters.AddWithValue("@dbName", targetDatabase);

            var exists = await checkCmd.ExecuteScalarAsync();
            if (exists == null)
            {
                logger.LogInformation("Creating PostgreSQL database '{Database}'...", targetDatabase);
                await using var createCmd = new NpgsqlCommand(
                    $"CREATE DATABASE \"{targetDatabase}\";", conn);
                await createCmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Note on database existence check: {Message}", ex.Message);
        }
    }

    private static async Task SeedOroPlataTenantAsync(JewerlyDbContext oroPlataDb, ITenantRegistry registry, ILogger logger)
    {
        try
        {
            // 1. Ensure Roles
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
            await oroPlataDb.SaveChangesAsync();

            // 2. Ensure User 'walvarez@ingnova.team' exists
            var user = await oroPlataDb.Users.FirstOrDefaultAsync(u => u.Email == "walvarez@ingnova.team");
            if (user == null)
            {
                // Try copying user details from Pomona DB if available
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

            // 3. Ensure Identification Types
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
}
