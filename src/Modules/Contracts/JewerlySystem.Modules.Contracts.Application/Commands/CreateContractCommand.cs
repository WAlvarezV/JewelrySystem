using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Contracts.Domain;

namespace JewerlySystem.Modules.Contracts.Application.Commands;

public record CreateContractCommand(
    int? Reference,
    DateTime Date,
    DateTime DeliveryDate,
    int Value,
    double Weight,
    string Description,
    int PersonId,
    int? ProviderId = null,
    int? FatherId = null) : ICommand<int>;

public class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractCommandValidator()
    {
        RuleFor(x => x.Value).GreaterThan(0).WithMessage("El valor del contrato debe ser mayor a 0.");
        RuleFor(x => x.Weight).GreaterThan(0).WithMessage("El peso debe ser mayor a 0.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("La descripción es obligatoria.");
        RuleFor(x => x.PersonId).GreaterThan(0).WithMessage("El cliente es obligatorio.");
        RuleFor(x => x.DeliveryDate).GreaterThanOrEqualTo(x => x.Date).WithMessage("La fecha de entrega debe ser posterior a la fecha del contrato.");
    }
}

public class CreateContractCommandHandler : ICommandHandler<CreateContractCommand, int>
{
    private readonly IContractRepository _repository;

    public CreateContractCommandHandler(IContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(CreateContractCommand request, CancellationToken cancellationToken)
    {
        var nextNumber = await _repository.GetNextContractNumberAsync(cancellationToken);

        var contract = new Contract(
            number: nextNumber,
            reference: request.Reference,
            date: request.Date,
            deliveryDate: request.DeliveryDate,
            value: request.Value,
            state: ContractState.Registrado,
            balance: request.Value,
            weight: request.Weight,
            description: request.Description,
            personId: request.PersonId,
            providerId: request.ProviderId,
            fatherId: request.FatherId
        );

        await _repository.AddAsync(contract, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(contract.Id);
    }
}
