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
    int? FatherId = null,
    int? InitialPayment = null,
    PaymentMethod PaymentMethod = PaymentMethod.EFECTIVO) : ICommand<int>;

public class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractCommandValidator()
    {
        RuleFor(x => x.Value).GreaterThan(0).WithMessage("El valor del contrato debe ser mayor a 0.");
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).WithMessage("El peso no puede ser negativo.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("La descripción es obligatoria.");
        RuleFor(x => x.PersonId).GreaterThan(0).WithMessage("El cliente es obligatorio.");
        RuleFor(x => x.DeliveryDate).GreaterThanOrEqualTo(x => x.Date).WithMessage("La fecha de entrega debe ser posterior a la fecha del contrato.");
        RuleFor(x => x.InitialPayment).GreaterThanOrEqualTo(0).When(x => x.InitialPayment.HasValue).WithMessage("El abono inicial no puede ser negativo.");
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

        if (request.InitialPayment.HasValue && request.InitialPayment.Value > 0)
        {
            contract.RegisterPayment(request.InitialPayment.Value);
        }

        await _repository.AddAsync(contract, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        if (request.InitialPayment.HasValue && request.InitialPayment.Value > 0)
        {
            var payment = new Payment(
                number: "1",
                date: request.Date,
                value: request.InitialPayment.Value,
                entityId: contract.Id,
                paymentType: PaymentType.Contrato,
                paymentMethod: request.PaymentMethod
            );
            await _repository.AddPaymentAsync(payment, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(contract.Id);
    }
}

public record UpdateContractCommand(
    int Id,
    int? Reference,
    DateTime DeliveryDate,
    string Description,
    ContractState State) : ICommand<bool>;

public class UpdateContractCommandValidator : AbstractValidator<UpdateContractCommand>
{
    public UpdateContractCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID de contrato inválido.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("La descripción es obligatoria.");
    }
}

public class UpdateContractCommandHandler : ICommandHandler<UpdateContractCommand, bool>
{
    private readonly IContractRepository _repository;

    public UpdateContractCommandHandler(IContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> Handle(UpdateContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (contract == null)
        {
            return Result.Failure<bool>(Error.NotFound("Contracts.NotFound", $"Contrato {request.Id} no encontrado."));
        }

        contract.Reference = request.Reference;
        contract.DeliveryDate = request.DeliveryDate;
        contract.Description = request.Description;
        contract.State = request.State;

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
