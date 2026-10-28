using JewerlySystem.Modules.Cash.Domain;

namespace JewerlySystem.Modules.Cash.Application;

public interface ICashRepository
{
    Task<DailyRecord?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DailyRecord>> ListAsync(DateTime? date = null, RecordType? type = null, CancellationToken ct = default);
    Task<ConsolidatedRecord> GetConsolidatedAsync(DateTime date, CancellationToken ct = default);
    Task<int> GetNextRecordNumberAsync(CancellationToken ct = default);
    Task AddAsync(DailyRecord record, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
