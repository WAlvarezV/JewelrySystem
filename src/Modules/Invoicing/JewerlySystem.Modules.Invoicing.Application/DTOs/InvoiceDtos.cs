namespace JewerlySystem.Modules.Invoicing.Application.DTOs;

public record InvoiceDto(
    int Id,
    string Number,
    DateTime Date,
    DateTime PaymentLimitDate,
    int Value,
    int CreditValue,
    int BalanceValue,
    int ProviderId,
    string Description);

public record InvoicePaymentDto(
    int Id,
    string Number,
    DateTime Date,
    int Value,
    int EntityId,
    string PaymentMethod);
