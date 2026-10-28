using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Contracts.Domain;

namespace JewerlySystem.Modules.Contracts.Application.Commands;

public record RegisterPaymentCommand(
    int ContractId,
    int Value,
    PaymentMethod PaymentMethod = PaymentMethod.EFECTIVO) : ICommand<int>;

public class RegisterPaymentCommandValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentCommandValidator()
    {
        RuleFor(x => x.ContractId).GreaterThan(0).WithMessage("El contrato es obligatorio.");
        RuleFor(x => x.Value).GreaterThan(0).WithMessage("El valor del pago debe ser mayor a 0.");
    }
}

public class RegisterPaymentCommandHandler : ICommandHandler<RegisterPaymentCommand, int>
{
    private readonly IContractRepository _repository;

    public RegisterPaymentCommandHandler(IContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(request.ContractId, cancellationToken);
        if (contract == null)
        {
            return Result.Failure<int>(Error.NotFound("Contracts.NotFound", $"Contrato con ID {request.ContractId} no encontrado."));
        }

        if (contract.State == ContractState.Recibido || contract.Balance <= 0)
        {
            return Result.Failure<int>(Error.Validation("Contracts.AlreadyPaid", "El contrato ya ha sido cancelado en su totalidad."));
        }

        var existingPayments = await _repository.GetPaymentsByContractIdAsync(request.ContractId, cancellationToken);
        var nextPaymentNumber = (existingPayments.Count + 1).ToString();

        var payment = new Payment(
            number: nextPaymentNumber,
            date: DateTime.UtcNow,
            value: request.Value,
            entityId: contract.Id,
            paymentType: PaymentType.Contrato,
            paymentMethod: request.PaymentMethod
        );

        contract.RegisterPayment(request.Value);

        await _repository.AddPaymentAsync(payment, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }
}
