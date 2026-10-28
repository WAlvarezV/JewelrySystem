using FluentValidation.TestHelper;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Customers.Application;
using JewerlySystem.Modules.Customers.Application.Commands;
using JewerlySystem.Modules.Customers.Domain;
using NSubstitute;
using Xunit;

namespace JewerlySystem.Modules.Customers.UnitTests;

public class CustomerTests
{
    [Fact]
    public async Task CreateCustomerCommandHandler_ShouldReturnConflict_WhenCustomerAlreadyExists()
    {
        // Arrange
        var repository = Substitute.For<ICustomerRepository>();
        var existing = new Person(1, "123456789", "Carlos Perez", "3001234567", "carlos@example.com", PersonType.Cliente);
        repository.GetByIdentificationAsync("123456789", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Person?>(existing));

        var handler = new CreateCustomerCommandHandler(repository);
        var command = new CreateCustomerCommand(1, "123456789", "Carlos Perez", "3001234567", "carlos@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task CreateCustomerCommandHandler_ShouldAddNewCustomer_WhenNotExists()
    {
        // Arrange
        var repository = Substitute.For<ICustomerRepository>();
        repository.GetByIdentificationAsync("987654321", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Person?>(null));

        var handler = new CreateCustomerCommandHandler(repository);
        var command = new CreateCustomerCommand(1, "987654321", "Maria Gomez", "3109876543", "maria@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).AddAsync(Arg.Is<Person>(p => p.IdentificationNumber == "987654321" && p.FullName == "Maria Gomez"), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateCustomerCommandValidator_ShouldHaveErrors_WhenFieldsAreEmpty()
    {
        // Arrange
        var validator = new CreateCustomerCommandValidator();
        var command = new CreateCustomerCommand(0, "", "", "", "");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdentificationTypeId);
        result.ShouldHaveValidationErrorFor(x => x.IdentificationNumber);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public async Task UpdateCustomerCommandHandler_ShouldUpdateFieldsSuccessfully()
    {
        // Arrange
        var repository = Substitute.For<ICustomerRepository>();
        var person = new Person(1, "10203040", "Juan Perez", "3000000000", "juan@test.com", PersonType.Cliente) { Id = 5 };
        repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(person);

        var handler = new UpdateCustomerCommandHandler(repository);
        var command = new UpdateCustomerCommand(5, 2, "10203040-9", "Juan Perez S.A.S.", "3111111111", "juan@corp.com", PersonType.Proveedor);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Juan Perez S.A.S.", person.FullName);
        Assert.Equal(PersonType.Proveedor, person.PersonType);
        Assert.Equal("10203040-9", person.IdentificationNumber);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCustomerByIdentificationQueryHandler_ShouldReturnDto_WhenFound()
    {
        // Arrange
        var repository = Substitute.For<ICustomerRepository>();
        var person = new Person(1, "55443322", "Ana Lopez", "3220000000", "ana@test.com", PersonType.Cliente) { Id = 8 };
        repository.GetByIdentificationAsync("55443322", Arg.Any<CancellationToken>()).Returns(person);

        var handler = new JewerlySystem.Modules.Customers.Application.Queries.GetCustomerByIdentificationQueryHandler(repository);

        // Act
        var result = await handler.Handle(new JewerlySystem.Modules.Customers.Application.Queries.GetCustomerByIdentificationQuery("55443322"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Ana Lopez", result.Value.FullName);
        Assert.Equal("55443322", result.Value.IdentificationNumber);
    }
}
