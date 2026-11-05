using FluentValidation.TestHelper;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Inventory.Application;
using JewerlySystem.Modules.Inventory.Application.Commands;
using JewerlySystem.Modules.Inventory.Application.DTOs;
using JewerlySystem.Modules.Inventory.Application.Queries;
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
    public async Task CreateJewelItemCommandHandler_ShouldDelegateToAtomicCreation_AndReturnAssignedReference()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        repository.CreateJewelItemAtomicAsync(Arg.Any<Item>(), Arg.Any<Jewel>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CreateItemResponse(10, 1)));

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
        Assert.Equal(10, result.Value.Id);
        Assert.Equal(1, result.Value.Reference);
        Assert.Equal("0001", result.Value.FormattedReference);
        await repository.Received(1).CreateJewelItemAtomicAsync(
            Arg.Is<Item>(i => i.CostValue == 80_000 && i.Description == "Anillo 18k"),
            Arg.Is<Jewel>(j => j.Weight == 5.2 && j.GramValue == 250_000),
            Arg.Any<CancellationToken>()
        );
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
    public async Task CreateWatchItemCommandHandler_ShouldDelegateToAtomicCreation_AndReturnAssignedReference()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        repository.CreateWatchItemAtomicAsync(Arg.Any<Item>(), Arg.Any<Watch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CreateItemResponse(25, 2)));

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
        Assert.Equal(25, result.Value.Id);
        Assert.Equal(2, result.Value.Reference);
        Assert.Equal("0002", result.Value.FormattedReference);
        await repository.Received(1).CreateWatchItemAtomicAsync(
            Arg.Is<Item>(i => i.CostValue == 1_200_000 && i.Description == "Reloj Tissot PRX"),
            Arg.Is<Watch>(w => w.CaseNumber == "T137.410.11" && w.BrandId == 2),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void CreateWatchItemCommandValidator_ShouldHaveError_WhenCostValueIsZeroOrNegative()
    {
        // Arrange
        var validator = new CreateWatchItemCommandValidator();
        var command = new CreateWatchItemCommand(0, DateTime.UtcNow, "Reloj", 5, GenderType.Hombre, "SN123", 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostValue);
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

    [Fact]
    public async Task ToggleItemActiveCommandHandler_ShouldTrackActivity_WithWeight1()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        var tracker = Substitute.For<JewerlySystem.Common.Application.IUserActivityTracker>();
        var item = new Item(reference: 555, costValue: 50_000, dateOfEntry: DateTime.UtcNow, description: "Pulsera", itemTypeId: 1) { Active = false };
        repository.GetItemByReferenceAsync(555, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new ToggleItemActiveCommandHandler(repository, tracker);

        // Act
        var result = await handler.Handle(new ToggleItemActiveCommand(555, true), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await tracker.Received(1).TrackActivityAsync(
            actionKey: ActionWeightRegistry.InventoryToggleActive,
            category: ActionWeightRegistry.CategoryAuditQuick,
            description: Arg.Any<string>(),
            weightScore: 1,
            entityName: "Item",
            entityReference: "555",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateJewelItemCommandHandler_ShouldTrackActivity_WithWeight10()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        var tracker = Substitute.For<JewerlySystem.Common.Application.IUserActivityTracker>();
        repository.CreateJewelItemAtomicAsync(Arg.Any<Item>(), Arg.Any<Jewel>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CreateItemResponse(100, 777)));

        var handler = new CreateJewelItemCommandHandler(repository, tracker);
        var command = new CreateJewelItemCommand(
            CostValue: 150_000,
            DateOfEntry: DateTime.UtcNow,
            Description: "Cadena Figaro 18k",
            ItemTypeId: 1,
            Weight: 7.5,
            GramValue: 260_000
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await tracker.Received(1).TrackActivityAsync(
            actionKey: ActionWeightRegistry.InventoryCreateJewel,
            category: ActionWeightRegistry.CategoryNewRecord,
            description: Arg.Any<string>(),
            weightScore: 10,
            entityName: "Item",
            entityReference: "777",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetItemHistoryQueryHandler_ShouldReturnTimelineWithWhoWhenAndWhatOperation()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        var item = new Item(
            reference: 505,
            costValue: 300_000,
            dateOfEntry: new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
            description: "Anillo de Esmeralda",
            itemTypeId: 1
        )
        {
            Id = 50,
            CreatedAt = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
            CreatedBy = "walvarez",
            LastModifiedAt = new DateTime(2026, 10, 20, 14, 30, 0, DateTimeKind.Utc),
            LastModifiedBy = "ggil"
        };

        var jewel = new Jewel(50, 4.8, 280_000);
        var itemTypes = new List<ItemType> { new(1, "Anillo") };

        var activityLogs = new List<UserActivityLog>
        {
            new("pomona", "user-1", "ggil", "INVENTORY", ActionWeightRegistry.InventoryUpdateJewel,
                ActionWeightRegistry.CategoryUpdate, "Actualizó peso de la joya Ref #505 a 4.8g", 4, "Item", "505")
            {
                Id = 1002,
                CreatedAt = new DateTime(2026, 10, 20, 14, 30, 0, DateTimeKind.Utc)
            },
            new("pomona", "user-2", "walvarez", "INVENTORY", ActionWeightRegistry.InventoryCreateJewel,
                ActionWeightRegistry.CategoryNewRecord, "Creó Joya Ref #505 - Anillo de Esmeralda (4.50g)", 10, "Item", "505")
            {
                Id = 1001,
                CreatedAt = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        repository.GetItemByReferenceAsync(505, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Item?>(item));
        repository.GetJewelByItemIdAsync(50, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Jewel?>(jewel));
        repository.GetWatchByItemIdAsync(50, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Watch?>(null));
        repository.GetItemTypesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ItemType>>(itemTypes));
        repository.GetBrandsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Brand>>(new List<Brand>()));
        repository.GetItemActivityLogsAsync("505", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<UserActivityLog>>(activityLogs));

        var handler = new GetItemHistoryQueryHandler(repository);
        var query = new GetItemHistoryQuery(505);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var history = result.Value;
        Assert.Equal(505, history.Reference);
        Assert.Equal("walvarez", history.CreatedBy);
        Assert.Equal("ggil", history.LastModifiedBy);
        Assert.NotEmpty(history.Events);

        // Verifica que los eventos contengan quién, cuándo y qué operación se hizo
        var firstEvent = history.Events.First(); // Más reciente
        Assert.Equal("ggil", firstEvent.PerformedBy);
        Assert.Equal("Actualización de Joya", firstEvent.Operation);
        Assert.Equal(new DateTime(2026, 10, 20, 14, 30, 0, DateTimeKind.Utc), firstEvent.Timestamp);

        var creationEvent = history.Events.Last(); // Creación
        Assert.Equal("walvarez", creationEvent.PerformedBy);
        Assert.Equal("Creación de Joya", creationEvent.Operation);
        Assert.Equal(new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc), creationEvent.Timestamp);
    }

    [Fact]
    public async Task GetItemHistoryQueryHandler_WhenItemNotFound_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var repository = Substitute.For<IInventoryRepository>();
        repository.GetItemByReferenceAsync(999, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Item?>(null));

        var handler = new GetItemHistoryQueryHandler(repository);
        var query = new GetItemHistoryQuery(999);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Items.NotFound", result.Error.Code);
    }
}
