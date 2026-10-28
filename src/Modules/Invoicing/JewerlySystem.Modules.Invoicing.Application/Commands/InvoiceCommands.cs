using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Invoicing.Domain;

namespace JewerlySystem.Modules.Invoicing.Application.Commands;

public record CreateInvoiceCommand(
    DateTime Date,
    DateTime PaymentLimitDate,
    int Value,
    int CreditValue,
    int ProviderId) : ICommand<int>;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.Value).GreaterThan(0).WithMessage("El valor de la factura debe ser mayor a 0.");
        RuleFor(x => x.ProviderId).GreaterThan(0).WithMessage("El proveedor es obligatorio.");
        RuleFor(x => x.PaymentLimitDate).GreaterThanOrEqualTo(x => x.Date).WithMessage("La fecha límite de pago debe ser posterior a la fecha de emisión.");
    }
}

public class CreateInvoiceCommandHandler : ICommandHandler<CreateInvoiceCommand, int>
{
    private readonly IInvoicingRepository _repository;

    public CreateInvoiceCommandHandler(IInvoicingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var nextNum = await _repository.GetNextInvoiceNumberAsync(cancellationToken);
        var balance = Math.Max(0, request.Value - request.CreditValue);

        var invoice = new Invoice(
            number: nextNum.ToString(),
            date: request.Date,
            paymentLimitDate: request.PaymentLimitDate,
            value: request.Value,
            creditValue: request.CreditValue,
            balanceValue: balance,
            providerId: request.ProviderId
        );

        await _repository.AddAsync(invoice, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(invoice.Id);
    }
}
