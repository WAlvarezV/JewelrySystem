using JewerlySystem.Modules.Invoicing.Domain;

namespace JewerlySystem.Modules.Invoicing.Application;

public interface IInvoicingRepository
{
    Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> ListAsync(int? providerId = null, CancellationToken ct = default);
    Task<IReadOnlyList<InvoicePayment>> GetPaymentsByInvoiceIdAsync(int invoiceId, CancellationToken ct = default);
    Task<int> GetNextInvoiceNumberAsync(CancellationToken ct = default);
    Task AddAsync(Invoice invoice, CancellationToken ct = default);
    Task AddPaymentAsync(InvoicePayment payment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
