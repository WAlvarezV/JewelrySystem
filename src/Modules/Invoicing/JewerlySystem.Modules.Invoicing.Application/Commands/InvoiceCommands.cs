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
    int ProviderId,
    string? Description = null,
    string? InvoiceNumber = null,
    string? InitialPaymentMethod = "EFECTIVO") : ICommand<int>;

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
        var invNumber = !string.IsNullOrWhiteSpace(request.InvoiceNumber)
            ? request.InvoiceNumber
            : (await _repository.GetNextInvoiceNumberAsync(cancellationToken)).ToString();

        var balance = Math.Max(0, request.Value - request.CreditValue);

        var invoice = new Invoice(
            number: invNumber,
            date: request.Date,
            paymentLimitDate: request.PaymentLimitDate,
            value: request.Value,
            creditValue: request.CreditValue,
            balanceValue: balance,
            providerId: request.ProviderId,
            description: request.Description
        );

        await _repository.AddAsync(invoice, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        if (request.CreditValue > 0)
        {
            var initialPayment = new InvoicePayment(
                number: "1",
                date: request.Date,
                value: request.CreditValue,
                entityId: invoice.Id,
                paymentMethod: request.InitialPaymentMethod ?? "EFECTIVO"
            );
            await _repository.AddPaymentAsync(initialPayment, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(invoice.Id);
    }
}

public record PayInvoiceCommand(
    int InvoiceId,
    int Amount,
    string PaymentMethod = "EFECTIVO") : ICommand<bool>;

public class PayInvoiceCommandValidator : AbstractValidator<PayInvoiceCommand>
{
    public PayInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).GreaterThan(0).WithMessage("ID de factura inválido.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("El valor del abono debe ser mayor a 0.");
    }
}

public class PayInvoiceCommandHandler : ICommandHandler<PayInvoiceCommand, bool>
{
    private readonly IInvoicingRepository _repository;

    public PayInvoiceCommandHandler(IInvoicingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> Handle(PayInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice == null)
        {
            return Result.Failure<bool>(Error.NotFound("Invoices.NotFound", $"Factura #{request.InvoiceId} no encontrada."));
        }

        var existingPayments = await _repository.GetPaymentsByInvoiceIdAsync(request.InvoiceId, cancellationToken);
        var paymentNum = (existingPayments.Count + 1).ToString();

        var payment = new InvoicePayment(
            number: paymentNum,
            date: DateTime.UtcNow,
            value: request.Amount,
            entityId: invoice.Id,
            paymentMethod: request.PaymentMethod
        );

        invoice.CreditValue += request.Amount;
        invoice.BalanceValue = Math.Max(0, invoice.Value - invoice.CreditValue);

        await _repository.AddPaymentAsync(payment, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
