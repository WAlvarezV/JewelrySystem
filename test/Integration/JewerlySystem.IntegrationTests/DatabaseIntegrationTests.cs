using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Modules.Auth.Domain;
using JewerlySystem.Modules.Cash.Domain;
using JewerlySystem.Modules.Contracts.Domain;
using JewerlySystem.Modules.Customers.Domain;
using JewerlySystem.Modules.Inventory.Domain;
using JewerlySystem.Modules.Invoicing.Domain;
using JewerlySystem.Web.Data;
using Microsoft.EntityFrameworkCore;
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
}
