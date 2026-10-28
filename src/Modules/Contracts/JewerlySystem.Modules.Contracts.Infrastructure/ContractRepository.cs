using JewerlySystem.Modules.Contracts.Application;
using JewerlySystem.Modules.Contracts.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Contracts.Infrastructure;

public class ContractRepository : IContractRepository
{
    private readonly ContractsDbContext _dbContext;

    public ContractRepository(ContractsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Contract?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Contracts.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<Contract>> ListAsync(int? personId = null, ContractState? state = null, CancellationToken ct = default)
    {
        var query = _dbContext.Contracts.AsNoTracking().AsQueryable();

        if (personId.HasValue)
        {
            query = query.Where(c => c.PersonId == personId.Value);
        }

        if (state.HasValue)
        {
            query = query.Where(c => c.State == state.Value);
        }

        return await query.OrderByDescending(c => c.Date).ToListAsync(ct);
    }

    public async Task<int> GetNextContractNumberAsync(CancellationToken ct = default)
    {
        var maxNumber = await _dbContext.Contracts.MaxAsync(c => (int?)c.Number, ct) ?? 0;
        return maxNumber + 1;
    }

    public async Task AddAsync(Contract contract, CancellationToken ct = default)
    {
        await _dbContext.Contracts.AddAsync(contract, ct);
    }

    public async Task AddPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        await _dbContext.Payments.AddAsync(payment, ct);
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsByContractIdAsync(int contractId, CancellationToken ct = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .Where(p => p.EntityId == contractId && p.PaymentType == PaymentType.Contrato)
            .OrderBy(p => p.Date)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
