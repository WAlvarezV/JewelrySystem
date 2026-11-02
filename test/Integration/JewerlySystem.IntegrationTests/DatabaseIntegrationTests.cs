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
}
