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
            CreditValue: 0,
            ProviderId: 10
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).AddAsync(
            Arg.Is<Invoice>(i => i.Number == "1001" && i.BalanceValue == 2_000_000 && i.ProviderId == 10),
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

    [Fact]
    public async Task CreateInvoiceCommandHandler_ShouldSaveDescription_AndCreateInitialPayment()
    {
        // Arrange
        var repository = Substitute.For<IInvoicingRepository>();
        var handler = new CreateInvoiceCommandHandler(repository);
        var command = new CreateInvoiceCommand(
            Date: DateTime.Today,
            PaymentLimitDate: DateTime.Today.AddDays(15),
            Value: 3_000_000,
            CreditValue: 1_000_000,
            ProviderId: 3,
            Description: "Lote de 5 cadenas de oro 18k",
            InvoiceNumber: "FAC-9001",
            InitialPaymentMethod: "TRANSFERENCIA"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).AddAsync(Arg.Is<Invoice>(i => i.Number == "FAC-9001" && i.Description == "Lote de 5 cadenas de oro 18k" && i.BalanceValue == 2_000_000), Arg.Any<CancellationToken>());
        await repository.Received(1).AddPaymentAsync(Arg.Is<InvoicePayment>(p => p.Value == 1_000_000 && p.PaymentMethod == "TRANSFERENCIA"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PayInvoiceCommandHandler_ShouldIncrementCreditValue_AndReduceBalance()
    {
        // Arrange
        var repository = Substitute.For<IInvoicingRepository>();
        var invoice = new Invoice("FAC-100", DateTime.Today, DateTime.Today.AddDays(30), 2_000_000, 500_000, 1_500_000, 1, "Compra relojes") { Id = 40 };
        repository.GetByIdAsync(40, Arg.Any<CancellationToken>()).Returns(invoice);
        repository.GetPaymentsByInvoiceIdAsync(40, Arg.Any<CancellationToken>()).Returns(new List<InvoicePayment>());

        var handler = new PayInvoiceCommandHandler(repository);
        var command = new PayInvoiceCommand(40, 500_000, "EFECTIVO");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1_000_000, invoice.CreditValue);
        Assert.Equal(1_000_000, invoice.BalanceValue);
        await repository.Received(1).AddPaymentAsync(Arg.Is<InvoicePayment>(p => p.Value == 500_000 && p.PaymentMethod == "EFECTIVO" && p.EntityId == 40), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInvoicePaymentsQueryHandler_ShouldReturnPaymentsList()
    {
        // Arrange
        var repository = Substitute.For<IInvoicingRepository>();
        var payments = new List<InvoicePayment>
        {
            new("1", DateTime.UtcNow, 300_000, 10, "TRANSFERENCIA"),
            new("2", DateTime.UtcNow, 200_000, 10, "ORO")
        };
        repository.GetPaymentsByInvoiceIdAsync(10, Arg.Any<CancellationToken>()).Returns(payments);

        var handler = new JewerlySystem.Modules.Invoicing.Application.Queries.GetInvoicePaymentsQueryHandler(repository);

        // Act
        var result = await handler.Handle(new JewerlySystem.Modules.Invoicing.Application.Queries.GetInvoicePaymentsQuery(10), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("ORO", result.Value[1].PaymentMethod);
        Assert.Equal(200_000, result.Value[1].Value);
    }
}
