using FluentValidation.TestHelper;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Contracts.Application;
using JewerlySystem.Modules.Contracts.Application.Commands;
using JewerlySystem.Modules.Contracts.Domain;
using NSubstitute;
using Xunit;

namespace JewerlySystem.Modules.Contracts.UnitTests;

public class ContractTests
{
    [Fact]
    public void RegisterPayment_ShouldDecrementBalance_AndSetRecibido_WhenBalanceReachesZero()
    {
        // Arrange
        var contract = new Contract(
            number: 101,
            reference: 555,
            date: DateTime.UtcNow,
            deliveryDate: DateTime.UtcNow.AddMonths(1),
            value: 500_000,
            state: ContractState.Registrado,
            balance: 500_000,
            weight: 12.5,
            description: "Cadena de oro 18k",
            personId: 1
        );

        // Act
        contract.RegisterPayment(500_000);

        // Assert
        Assert.Equal(0, contract.Balance);
        Assert.Equal(ContractState.Recibido, contract.State);
    }

    [Fact]
    public void RegisterPayment_ShouldDecrementBalance_WithoutChangingState_WhenBalanceRemains()
    {
        // Arrange
        var contract = new Contract(
            number: 102,
            reference: null,
            date: DateTime.UtcNow,
            deliveryDate: DateTime.UtcNow.AddMonths(1),
            value: 1_000_000,
            state: ContractState.Registrado,
            balance: 1_000_000,
            weight: 20.0,
            description: "Anillo de oro",
            personId: 2
        );

        // Act
        contract.RegisterPayment(300_000);

        // Assert
        Assert.Equal(700_000, contract.Balance);
        Assert.Equal(ContractState.Registrado, contract.State);
    }

    [Fact]
    public async Task CreateContractCommandHandler_ShouldAddNewContract_WithNextContractNumber()
    {
        // Arrange
        var repository = Substitute.For<IContractRepository>();
        repository.GetNextContractNumberAsync(Arg.Any<CancellationToken>()).Returns(105);

        var handler = new CreateContractCommandHandler(repository);
        var command = new CreateContractCommand(
            Reference: 999,
            Date: DateTime.UtcNow,
            DeliveryDate: DateTime.UtcNow.AddDays(30),
            Value: 400_000,
            Weight: 8.5,
            Description: "Pulsera de oro",
            PersonId: 3
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).AddAsync(Arg.Is<Contract>(c => c.Number == 105 && c.Value == 400_000), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterPaymentCommandHandler_ShouldReturnNotFound_WhenContractDoesNotExist()
    {
        // Arrange
        var repository = Substitute.For<IContractRepository>();
        repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Contract?>(null));

        var handler = new RegisterPaymentCommandHandler(repository);
        var command = new RegisterPaymentCommand(999, 100_000);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task RegisterPaymentCommandHandler_ShouldSucceed_AndReduceContractBalance()
    {
        // Arrange
        var repository = Substitute.For<IContractRepository>();
        var contract = new Contract(
            number: 10,
            reference: null,
            date: DateTime.UtcNow,
            deliveryDate: DateTime.UtcNow.AddDays(15),
            value: 500_000,
            state: ContractState.Registrado,
            balance: 500_000,
            weight: 10.0,
            description: "Joya",
            personId: 1
        )
        {
            Id = 10
        };

        repository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Contract?>(contract));
        repository.GetPaymentsByContractIdAsync(10, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<Payment>>(new List<Payment>()));

        var handler = new RegisterPaymentCommandHandler(repository);
        var command = new RegisterPaymentCommand(10, 200_000);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(300_000, contract.Balance);
        await repository.Received(1).AddPaymentAsync(Arg.Is<Payment>(p => p.Value == 200_000 && p.EntityId == 10), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateContractCommandValidator_ShouldHaveError_WhenValueIsZeroOrNegative()
    {
        // Arrange
        var validator = new CreateContractCommandValidator();
        var command = new CreateContractCommand(null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 0, 5.0, "Desc", 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value);
    }
}
