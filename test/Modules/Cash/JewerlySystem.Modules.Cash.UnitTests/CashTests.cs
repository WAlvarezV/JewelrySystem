using FluentValidation.TestHelper;
using JewerlySystem.Modules.Cash.Application;
using JewerlySystem.Modules.Cash.Application.Commands;
using JewerlySystem.Modules.Cash.Domain;
using NSubstitute;
using Xunit;

namespace JewerlySystem.Modules.Cash.UnitTests;

public class CashTests
{
    [Fact]
    public void ConsolidatedRecord_Balance_ShouldCalculateDifferenceCorrectly()
    {
        // Arrange
        var consolidated = new ConsolidatedRecord
        {
            Date = DateTime.Today,
            TotalIngresos = 1_500_000,
            TotalEgresos = 600_000
        };

        // Assert
        Assert.Equal(900_000, consolidated.Balance);
    }

    [Fact]
    public async Task CreateDailyRecordCommandHandler_ShouldSaveRecordWithNextNumber()
    {
        // Arrange
        var repository = Substitute.For<ICashRepository>();
        repository.GetNextRecordNumberAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(55));

        var handler = new CreateDailyRecordCommandHandler(repository);
        var command = new CreateDailyRecordCommand(
            Date: DateTime.UtcNow,
            Reference: 1234,
            Description: "Venta de joya al contado",
            Value: 350_000,
            RecordType: RecordType.INGRESO,
            PaymentMethod: PaymentMethod.EFECTIVO
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).AddAsync(
            Arg.Is<DailyRecord>(r => r.Number == "55" && r.Value == 350_000 && r.RecordType == RecordType.INGRESO),
            Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateDailyRecordCommandValidator_ShouldHaveErrors_WhenValueIsZeroOrDescriptionIsEmpty()
    {
        // Arrange
        var validator = new CreateDailyRecordCommandValidator();
        var command = new CreateDailyRecordCommand(DateTime.UtcNow, null, "", 0, RecordType.EGRESO, PaymentMethod.EFECTIVO);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public async Task CreateDailyRecordCommandHandler_ShouldUseProvidedNumber_WhenSpecified()
    {
        // Arrange
        var repository = Substitute.For<ICashRepository>();
        var handler = new CreateDailyRecordCommandHandler(repository);
        var command = new CreateDailyRecordCommand(
            Date: DateTime.UtcNow,
            Reference: 99,
            Description: "Abono contrato",
            Value: 150_000,
            RecordType: RecordType.INGRESO,
            PaymentMethod: PaymentMethod.TRANSFERENCIA,
            Number: "CTR-105"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).AddAsync(Arg.Is<DailyRecord>(r => r.Number == "CTR-105" && r.PaymentMethod == PaymentMethod.TRANSFERENCIA), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCashConsolidatedRangeQueryHandler_ShouldAggregateMovementsByDateCorrectly()
    {
        // Arrange
        var repository = Substitute.For<ICashRepository>();
        var testDate = new DateTime(2026, 10, 25);
        var records = new List<DailyRecord>
        {
            new(testDate, "1", null, "Venta efectivo", 200_000, RecordType.INGRESO, PaymentMethod.EFECTIVO),
            new(testDate, "2", null, "Venta tarjeta", 300_000, RecordType.INGRESO, PaymentMethod.TARJETA_CREDITO),
            new(testDate, "3", null, "Pago luz", 50_000, RecordType.EGRESO, PaymentMethod.EFECTIVO)
        };

        repository.ListAsync(Arg.Any<DateTime?>(), Arg.Any<RecordType?>(), Arg.Any<CancellationToken>()).Returns(records);

        var handler = new JewerlySystem.Modules.Cash.Application.Queries.GetCashConsolidatedRangeQueryHandler(repository);
        var query = new JewerlySystem.Modules.Cash.Application.Queries.GetCashConsolidatedRangeQuery(testDate.AddDays(-1), testDate.AddDays(1));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        var item = result.Value[0];
        Assert.Equal(200_000, item.CashInValue);
        Assert.Equal(300_000, item.OthersInValue);
        Assert.Equal(500_000, item.TotalInValue);
        Assert.Equal(50_000, item.CashOutValue);
        Assert.Equal(450_000, item.TotalNetValue);
        Assert.Equal(150_000, item.CashBalanceValue); // 200_000 efectivo - 50_000 egreso
    }
}
