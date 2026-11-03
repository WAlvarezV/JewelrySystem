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

    [Fact]
    public async Task CreateWatchItemCommandHandler_ShouldAddWatchAndItem_Successfully()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        repository.GetNextItemReferenceAsync(Arg.Any<CancellationToken>()).Returns(505);

        var handler = new CreateWatchItemCommandHandler(repository);
        var command = new CreateWatchItemCommand(
            CostValue: 1_200_000,
            DateOfEntry: DateTime.UtcNow,
            Description: "Reloj Tissot PRX",
            ItemTypeId: 5,
            GenderType: GenderType.Hombre,
            CaseNumber: "T137.410.11",
            BrandId: 2
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).AddItemAsync(Arg.Is<Item>(i => i.Reference == 505 && i.CostValue == 1_200_000), Arg.Any<CancellationToken>());
        await repository.Received(1).AddWatchAsync(Arg.Is<Watch>(w => w.CaseNumber == "T137.410.11" && w.BrandId == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DischargeItemCommandHandler_ShouldMarkItemInactive_AndSetSaleDetails()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        var item = new Item(100, 50_000, DateTime.UtcNow.AddDays(-10), "Anillo", 1) { Id = 10 };
        repository.GetItemByIdAsync(10, Arg.Any<CancellationToken>()).Returns(item);

        var handler = new DischargeItemCommandHandler(repository);
        var command = new DischargeItemCommand(10, DateTime.UtcNow, 85_000);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(item.Active);
        Assert.Equal(85_000, item.SaleValue);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetItemByReferenceQueryHandler_ShouldReturnDetailedItem_WhenExists()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        var item = new Item(777, 200_000, DateTime.UtcNow, "Dije de oro", 6) { Id = 20 };
        var jewel = new Jewel(20, 3.5, 220_000);

        repository.GetItemByReferenceAsync(777, Arg.Any<CancellationToken>()).Returns(item);
        repository.GetJewelByItemIdAsync(20, Arg.Any<CancellationToken>()).Returns(jewel);
        repository.GetItemTypesAsync(Arg.Any<CancellationToken>()).Returns(new List<ItemType> { new(6, "Dijes") });

        var handler = new JewerlySystem.Modules.Inventory.Application.Queries.GetItemByReferenceQueryHandler(repository);

        // Act
        var result = await handler.Handle(new JewerlySystem.Modules.Inventory.Application.Queries.GetItemByReferenceQuery(777), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(777, result.Value.Reference);
        Assert.Equal("Dijes", result.Value.ItemTypeName);
        Assert.NotNull(result.Value.Jewel);
        Assert.Equal(3.5, result.Value.Jewel.Weight);
    }

    [Fact]
    public async Task ToggleItemActiveCommandHandler_ShouldActivateItem_AndClearDateOfSale()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        var item = new Item(888, 150_000, DateTime.UtcNow.AddDays(-20), "Cadena", 2)
        {
            Id = 30,
            Active = false,
            DateOfSale = DateTime.UtcNow.AddDays(-5),
            SaleValue = 200_000
        };

        repository.GetItemByReferenceAsync(888, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new ToggleItemActiveCommandHandler(repository);

        // Act
        var result = await handler.Handle(new ToggleItemActiveCommand(888, true), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(item.Active);
        Assert.Null(item.DateOfSale);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleItemActiveCommandHandler_ShouldDeactivateItem_WhenFalse()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        var item = new Item(999, 300_000, DateTime.UtcNow, "Reloj", 5)
        {
            Id = 40,
            Active = true
        };

        repository.GetItemByReferenceAsync(999, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new ToggleItemActiveCommandHandler(repository);

        // Act
        var result = await handler.Handle(new ToggleItemActiveCommand(999, false), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(item.Active);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleItemActiveCommandHandler_ShouldReturnNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        repository.GetItemByReferenceAsync(1234, Arg.Any<CancellationToken>()).Returns((Item?)null);
        var handler = new ToggleItemActiveCommandHandler(repository);

        // Act
        var result = await handler.Handle(new ToggleItemActiveCommand(1234, true), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Items.NotFound", result.Error.Code);
    }
}
