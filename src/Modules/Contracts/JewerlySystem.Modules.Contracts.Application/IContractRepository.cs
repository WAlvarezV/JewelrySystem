using JewerlySystem.Modules.Contracts.Domain;

namespace JewerlySystem.Modules.Contracts.Application;

public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Contract>> ListAsync(int? personId = null, ContractState? state = null, CancellationToken ct = default);
    Task<int> GetNextContractNumberAsync(CancellationToken ct = default);
    Task AddAsync(Contract contract, CancellationToken ct = default);
    Task AddPaymentAsync(Payment payment, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetPaymentsByContractIdAsync(int contractId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
