using FluentValidation.TestHelper;
using JewerlySystem.Modules.Inventory.Application;
using JewerlySystem.Modules.Inventory.Application.Commands;
using JewerlySystem.Modules.Inventory.Domain;
using NSubstitute;
using Xunit;

namespace JewerlySystem.Modules.Inventory.UnitTests;

public class InventoryTests
{
    [Fact]
    public void Discharge_ShouldSetInactive_AndRecordSaleDetails()
    {
        // Arrange
        var item = new Item(
            reference: 101,
            costValue: 100_000,
            dateOfEntry: DateTime.UtcNow.AddMonths(-1),
            description: "Cadena de oro",
            itemTypeId: 1
        );

        var saleDate = DateTime.UtcNow;

        // Act
        item.Discharge(saleDate, 150_000);

        // Assert
        Assert.False(item.Active);
        Assert.Equal(saleDate, item.DateOfSale);
        Assert.Equal(150_000, item.SaleValue);
    }

    [Fact]
    public async Task CreateJewelItemCommandHandler_ShouldAddItemAndJewel_WithNextReference()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        repository.GetNextItemReferenceAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(200));

        var handler = new CreateJewelItemCommandHandler(repository);
        var command = new CreateJewelItemCommand(
            CostValue: 80_000,
            DateOfEntry: DateTime.UtcNow,
            Description: "Anillo 18k",
            ItemTypeId: 1,
            Weight: 5.2,
            GramValue: 250_000
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).AddItemAsync(Arg.Is<Item>(i => i.Reference == 200 && i.CostValue == 80_000), Arg.Any<CancellationToken>());
        await repository.Received(1).AddJewelAsync(Arg.Is<Jewel>(j => j.Weight == 5.2), Arg.Any<CancellationToken>());
        await repository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateJewelItemCommandValidator_ShouldHaveError_WhenWeightIsZeroOrNegative()
    {
        // Arrange
        var validator = new CreateJewelItemCommandValidator();
        var command = new CreateJewelItemCommand(100_000, DateTime.UtcNow, "D", 1, 0, 100);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Weight);
    }
}
