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
}
