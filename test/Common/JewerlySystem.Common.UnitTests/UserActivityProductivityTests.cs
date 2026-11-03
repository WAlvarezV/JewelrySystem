using JewerlySystem.Common.Domain;
using Xunit;

namespace JewerlySystem.Common.UnitTests;

public class UserActivityProductivityTests
{
    [Fact]
    public void ActionWeightRegistry_ShouldAssignHigherWeightToCreationThanActivation()
    {
        // Act
        var jewelWeight = ActionWeightRegistry.GetDefaultWeight(ActionWeightRegistry.InventoryCreateJewel);
        var watchWeight = ActionWeightRegistry.GetDefaultWeight(ActionWeightRegistry.InventoryCreateWatch);
        var updateWeight = ActionWeightRegistry.GetDefaultWeight(ActionWeightRegistry.InventoryUpdateJewel);
        var toggleWeight = ActionWeightRegistry.GetDefaultWeight(ActionWeightRegistry.InventoryToggleActive);

        // Assert
        Assert.Equal(10, jewelWeight);
        Assert.Equal(10, watchWeight);
        Assert.Equal(4, updateWeight);
        Assert.Equal(1, toggleWeight);

        // Creación debe pesar significativamente más que la simple activación en lista (10x)
        Assert.True(jewelWeight > toggleWeight);
        Assert.True(watchWeight > toggleWeight);
        Assert.True(jewelWeight > updateWeight);
    }

    [Fact]
    public void ActionWeightRegistry_ShouldDefineAllEssentialMetadata()
    {
        // Act
        var def = ActionWeightRegistry.GetDefinition(ActionWeightRegistry.InventoryCreateJewel);

        // Assert
        Assert.NotNull(def);
        Assert.Equal("INVENTORY", def.ModuleKey);
        Assert.Equal(ActionWeightRegistry.CategoryNewRecord, def.Category);
        Assert.False(string.IsNullOrWhiteSpace(def.Icon));
        Assert.False(string.IsNullOrWhiteSpace(def.ColorHex));
    }

    [Fact]
    public void UserActivityLog_ShouldConstructWithAllProperties()
    {
        // Arrange & Act
        var log = new UserActivityLog(
            tenantId: "pomona",
            userId: "user-123",
            userName: "vendedor@pomona.com",
            moduleKey: "INVENTORY",
            actionKey: ActionWeightRegistry.InventoryCreateJewel,
            category: ActionWeightRegistry.CategoryNewRecord,
            description: "Creó Joya Ref #100",
            weightScore: 10,
            entityName: "Item",
            entityReference: "100"
        );

        // Assert
        Assert.Equal("pomona", log.TenantId);
        Assert.Equal("user-123", log.UserId);
        Assert.Equal("vendedor@pomona.com", log.UserName);
        Assert.Equal("INVENTORY", log.ModuleKey);
        Assert.Equal(ActionWeightRegistry.InventoryCreateJewel, log.ActionKey);
        Assert.Equal(ActionWeightRegistry.CategoryNewRecord, log.Category);
        Assert.Equal(10, log.WeightScore);
        Assert.Equal("100", log.EntityReference);
        Assert.True(log.CreatedAt <= DateTime.UtcNow);
    }
}
