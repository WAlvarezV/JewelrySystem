using JewerlySystem.Modules.Invoicing.Application;
using JewerlySystem.Modules.Invoicing.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Invoicing.Infrastructure;

public class InvoicingRepository : IInvoicingRepository
{
    private readonly InvoicingDbContext _dbContext;

    public InvoicingRepository(InvoicingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<IReadOnlyList<Invoice>> ListAsync(int? providerId = null, CancellationToken ct = default)
    {
        var query = _dbContext.Invoices.AsNoTracking().AsQueryable();

        if (providerId.HasValue)
        {
            query = query.Where(i => i.ProviderId == providerId.Value);
        }

        return await query.OrderByDescending(i => i.Date).ToListAsync(ct);
    }

    public async Task<int> GetNextInvoiceNumberAsync(CancellationToken ct = default)
    {
        var invoices = await _dbContext.Invoices.AsNoTracking().ToListAsync(ct);
        var maxNum = invoices
            .Select(i => int.TryParse(i.Number, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return maxNum + 1;
    }

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default)
    {
        await _dbContext.Invoices.AddAsync(invoice, ct);
    }

    public async Task<IReadOnlyList<InvoicePayment>> GetPaymentsByInvoiceIdAsync(int invoiceId, CancellationToken ct = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .Where(p => p.EntityId == invoiceId && p.PaymentType == "Factura")
            .OrderBy(p => p.Date)
            .ToListAsync(ct);
    }

    public async Task AddPaymentAsync(InvoicePayment payment, CancellationToken ct = default)
    {
        await _dbContext.Payments.AddAsync(payment, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
