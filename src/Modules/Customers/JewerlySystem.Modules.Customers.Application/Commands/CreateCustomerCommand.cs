using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Customers.Domain;

namespace JewerlySystem.Modules.Customers.Application.Commands;

public record CreateCustomerCommand(
    int IdentificationTypeId,
    string IdentificationNumber,
    string FullName,
    string CellPhone,
    string Email,
    PersonType PersonType = PersonType.Cliente) : ICommand<int>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.IdentificationTypeId).GreaterThan(0).WithMessage("El tipo de identificación es obligatorio.");
        RuleFor(x => x.IdentificationNumber).NotEmpty().WithMessage("El número de identificación es obligatorio.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("El nombre completo es obligatorio.");
    }
}

public class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, int>
{
    private readonly ICustomerRepository _repository;

    public CreateCustomerCommandHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdentificationAsync(request.IdentificationNumber, cancellationToken);
        if (existing != null)
        {
            return Result.Failure<int>(Error.Conflict("Customers.Duplicate", $"Ya existe un cliente con número de identificación {request.IdentificationNumber}."));
        }

        var person = new Person(
            identificationTypeId: request.IdentificationTypeId,
            identificationNumber: request.IdentificationNumber,
            fullName: request.FullName,
            cellPhone: request.CellPhone,
            email: request.Email,
            personType: request.PersonType
        );

        await _repository.AddAsync(person, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(person.Id);
    }
}

public record UpdateCustomerCommand(
    int Id,
    int IdentificationTypeId,
    string IdentificationNumber,
    string FullName,
    string CellPhone,
    string Email,
    PersonType PersonType) : ICommand<bool>;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID inválido.");
        RuleFor(x => x.IdentificationNumber).NotEmpty().WithMessage("El número de identificación es obligatorio.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("El nombre completo es obligatorio.");
    }
}

public class UpdateCustomerCommandHandler : ICommandHandler<UpdateCustomerCommand, bool>
{
    private readonly ICustomerRepository _repository;

    public UpdateCustomerCommandHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var person = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (person == null)
        {
            return Result.Failure<bool>(Error.NotFound("Customers.NotFound", $"Persona con ID {request.Id} no encontrada."));
        }

        person.IdentificationTypeId = request.IdentificationTypeId;
        person.IdentificationNumber = request.IdentificationNumber;
        person.FullName = request.FullName;
        person.CellPhone = request.CellPhone;
        person.Email = request.Email;
        person.PersonType = request.PersonType;

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
