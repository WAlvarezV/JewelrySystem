using FluentValidation.TestHelper;
using JewerlySystem.Modules.Invoicing.Application;
using JewerlySystem.Modules.Invoicing.Application.Commands;
using JewerlySystem.Modules.Invoicing.Domain;
using NSubstitute;
using Xunit;

namespace JewerlySystem.Modules.Invoicing.UnitTests;

public class InvoicingTests
{
    [Fact]
    public async Task CreateInvoiceCommandHandler_ShouldCalculateBalance_AndSaveWithNextNumber()
    {
        // Arrange
        var repository = Substitute.For<IInvoicingRepository>();
        repository.GetNextInvoiceNumberAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1001));

        var handler = new CreateInvoiceCommandHandler(repository);
        var command = new CreateInvoiceCommand(
            Date: DateTime.Today,
            PaymentLimitDate: DateTime.Today.AddDays(30),
            Value: 2_000_000,
            CreditValue: 500_000,
            ProviderId: 10
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).AddAsync(
            Arg.Is<Invoice>(i => i.Number == "1001" && i.BalanceValue == 1_500_000 && i.ProviderId == 10),
            Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateInvoiceCommandValidator_ShouldHaveErrors_WhenValueIsZeroOrProviderIsZero()
    {
        // Arrange
        var validator = new CreateInvoiceCommandValidator();
        var command = new CreateInvoiceCommand(DateTime.Today, DateTime.Today.AddDays(1), 0, 0, 0);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value);
        result.ShouldHaveValidationErrorFor(x => x.ProviderId);
    }
}
