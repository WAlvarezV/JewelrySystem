using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Common.Infrastructure.Tenant;
using JewerlySystem.Modules.Auth.Domain;
using JewerlySystem.Modules.Cash.Domain;
using JewerlySystem.Modules.Contracts.Domain;
using JewerlySystem.Modules.Customers.Domain;
using JewerlySystem.Modules.Inventory.Domain;
using JewerlySystem.Modules.Invoicing.Domain;
using JewerlySystem.Web.Data;
using JewerlySystem.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JewerlySystem.IntegrationTests;

public class DatabaseIntegrationTests
{
    private JewerlyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewerlyDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=IntZaioNJewerly_Test;Username=postgres;Password=IntZaioN;")
            .Options;

        return new JewerlyDbContext(options);
    }

    [Fact]
    public void DbContext_ShouldContainAllModularMonolithEntityTypes()
    {
        // Arrange
        using var context = CreateContext();
        var model = context.Model;

        // Assert - Auth & Passkeys
        Assert.NotNull(model.FindEntityType(typeof(PasskeyCredential)));

        // Assert - Contracts & Payments
        Assert.NotNull(model.FindEntityType(typeof(Contract)));
        Assert.NotNull(model.FindEntityType(typeof(Payment)));

        // Assert - Inventory
        Assert.NotNull(model.FindEntityType(typeof(Item)));
        Assert.NotNull(model.FindEntityType(typeof(Jewel)));
        Assert.NotNull(model.FindEntityType(typeof(Watch)));
        Assert.NotNull(model.FindEntityType(typeof(Brand)));
        Assert.NotNull(model.FindEntityType(typeof(ItemType)));

        // Assert - Cash
        Assert.NotNull(model.FindEntityType(typeof(DailyRecord)));

        // Assert - Customers
        Assert.NotNull(model.FindEntityType(typeof(Person)));
        Assert.NotNull(model.FindEntityType(typeof(IdentificationType)));

        // Assert - Invoicing
        Assert.NotNull(model.FindEntityType(typeof(Invoice)));
    }

    [Fact]
    public void ContractsEntity_ShouldMapToContractsTable_WithExpectedColumns()
    {
        // Arrange
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Contract));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal("Contracts", entityType.GetTableName());
        Assert.NotNull(entityType.FindProperty(nameof(Contract.Number)));
        Assert.NotNull(entityType.FindProperty(nameof(Contract.State)));
        Assert.NotNull(entityType.FindProperty(nameof(Contract.Balance)));
    }

    [Fact]
    public void PasskeyCredentialEntity_ShouldMapToPasskeyCredentialsTable()
    {
        // Arrange
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(PasskeyCredential));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal("PasskeyCredentials", entityType.GetTableName());
        Assert.NotNull(entityType.FindProperty(nameof(PasskeyCredential.CredentialId)));
        Assert.NotNull(entityType.FindProperty(nameof(PasskeyCredential.PublicKey)));
    }

    [Fact]
    public void JewelEntity_ShouldMapToJewelryTable()
    {
        // Arrange
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Jewel));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal("Jewelry", entityType.GetTableName());
        Assert.NotNull(entityType.FindProperty(nameof(Jewel.Weight)));
        Assert.NotNull(entityType.FindProperty(nameof(Jewel.GramValue)));
    }

    [Fact]
    public async Task Ensure_JoglisUser_Is_Seeded_In_Pomona()
    {
        var options = new DbContextOptionsBuilder<JewerlyDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN;")
            .Options;

        await using var db = new JewerlyDbContext(options);
        var vendedorRoleId = "90e87e25-6d98-4d3b-ab05-38ea72d7ccfa";
        var email = "joglisjjc@gmail.com";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            var userId = Guid.NewGuid().ToString();
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<JewerlySystem.Modules.Auth.Infrastructure.ApplicationUser>();
            var newUser = new JewerlySystem.Modules.Auth.Infrastructure.ApplicationUser
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
            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
            {
                UserId = userId,
                RoleId = vendedorRoleId
            });
            await db.SaveChangesAsync();
        }

        var check = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(check);
    }

    [Fact]
    public async Task Ensure_All_Tenants_Have_Active_Modules_And_Menus_And_Admins()
    {
        var config = new ConfigurationBuilder().Build();
        var registry = new TenantRegistry(config);
        var logger = NullLogger<JewerlyDbContext>.Instance;
        var defaultTenant = registry.GetDefaultTenant();

        foreach (var tenant in registry.GetAllTenants())
        {
            // 1. Ensure DB exists in PostgreSQL
            await DatabaseExtensions.EnsureDatabaseCreatedAsync(tenant.ConnectionString, defaultTenant.ConnectionString, logger);

            // 2. Ensure schema
            var options = new DbContextOptionsBuilder<JewerlyDbContext>()
                .UseNpgsql(tenant.ConnectionString)
                .Options;

            await using var tenantDb = new JewerlyDbContext(options);
            await tenantDb.Database.EnsureCreatedAsync();

            // 3. Ensure baseline
            await DatabaseExtensions.EnsureTenantBaselineAsync(tenantDb, tenant, registry, logger);

            // 4. Assert modules
            var moduleCount = await tenantDb.Set<AppModule>().CountAsync();
            var activeModuleCount = await tenantDb.Set<AppModule>().CountAsync(m => m.IsActive);
            Assert.Equal(6, moduleCount);
            Assert.Equal(6, activeModuleCount);

            // 5. Assert menus
            var menuCount = await tenantDb.Set<MenuItem>().CountAsync();
            var activeMenuCount = await tenantDb.Set<MenuItem>().CountAsync(m => m.IsActive);
            Assert.True(menuCount >= 21, $"Tenant {tenant.Id} has {menuCount} menus, expected >= 21");
            Assert.True(activeMenuCount >= 21, $"Tenant {tenant.Id} has {activeMenuCount} active menus, expected >= 21");

            // 6. Assert roles
            var roleCount = await tenantDb.Roles.CountAsync();
            Assert.True(roleCount >= 6, $"Tenant {tenant.Id} has {roleCount} roles, expected >= 6");

            // 7. Assert users
            string[] requiredAdmins = ["walvarez@ingnova.team", "walvarez@bezion.tech", "jmalvarezc@bezion.tech", "admin@joyeriapomona.com"];
            foreach (var email in requiredAdmins)
            {
                var user = await tenantDb.Users.FirstOrDefaultAsync(u => u.Email == email);
                Assert.NotNull(user);
                Assert.True(user.IsActive, $"User {email} in {tenant.Id} should be active");
            }

            // 8. Assert permissions exist
            var permCount = await tenantDb.Set<RolePermission>().CountAsync();
            Assert.True(permCount >= 21, $"Tenant {tenant.Id} has {permCount} permissions, expected >= 21");
        }
    }
}
