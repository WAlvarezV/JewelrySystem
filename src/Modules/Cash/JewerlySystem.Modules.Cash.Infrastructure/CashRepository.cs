using JewerlySystem.Modules.Cash.Application;
using JewerlySystem.Modules.Cash.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Cash.Infrastructure;

public class CashRepository : ICashRepository
{
    private readonly CashDbContext _dbContext;

    public CashRepository(CashDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyRecord?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.DailyRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<DailyRecord>> ListAsync(DateTime? date = null, RecordType? type = null, CancellationToken ct = default)
    {
        var query = _dbContext.DailyRecords.AsNoTracking().AsQueryable();

        if (date.HasValue)
        {
            var targetDate = date.Value.Date;
            query = query.Where(r => r.Date.Date == targetDate);
        }

        if (type.HasValue)
        {
            query = query.Where(r => r.RecordType == type.Value);
        }

        return await query.OrderByDescending(r => r.Date).ToListAsync(ct);
    }

    public async Task<ConsolidatedRecord> GetConsolidatedAsync(DateTime date, CancellationToken ct = default)
    {
        var targetDate = date.Date;
        var records = await _dbContext.DailyRecords
            .AsNoTracking()
            .Where(r => r.Date.Date == targetDate)
            .ToListAsync(ct);

        var totalIngresos = records.Where(r => r.RecordType == RecordType.INGRESO).Sum(r => r.Value);
        var totalEgresos = records.Where(r => r.RecordType == RecordType.EGRESO).Sum(r => r.Value);

        return new ConsolidatedRecord
        {
            Date = targetDate,
            TotalIngresos = totalIngresos,
            TotalEgresos = totalEgresos
        };
    }

    public async Task<int> GetNextRecordNumberAsync(CancellationToken ct = default)
    {
        var records = await _dbContext.DailyRecords.AsNoTracking().ToListAsync(ct);
        var maxNum = records
            .Select(r => int.TryParse(r.Number, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return maxNum + 1;
    }

    public async Task AddAsync(DailyRecord record, CancellationToken ct = default)
    {
        await _dbContext.DailyRecords.AddAsync(record, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
