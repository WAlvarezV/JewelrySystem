using System.Linq;
using System.Linq.Expressions;
using JewerlySystem.Modules.Invoicing.Application.DTOs;
using JewerlySystem.Modules.Invoicing.Domain;

namespace JewerlySystem.Modules.Invoicing.Application.DTOs;

public static class InvoiceMappingExtensions
{
    public static readonly Expression<Func<Invoice, InvoiceDto>> InvoiceDtoProjection = i => new InvoiceDto(
        i.Id,
        i.Number,
        i.Date,
        i.PaymentLimitDate,
        i.Value,
        i.CreditValue,
        i.BalanceValue,
        i.ProviderId
    );

    public static InvoiceDto ToDto(this Invoice invoice) => new(
        invoice.Id,
        invoice.Number,
        invoice.Date,
        invoice.PaymentLimitDate,
        invoice.Value,
        invoice.CreditValue,
        invoice.BalanceValue,
        invoice.ProviderId
    );

    public static IQueryable<InvoiceDto> ProjectToDto(this IQueryable<Invoice> query) => query.Select(InvoiceDtoProjection);
}
