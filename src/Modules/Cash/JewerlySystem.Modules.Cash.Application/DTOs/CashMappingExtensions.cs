using System.Linq;
using System.Linq.Expressions;
using JewerlySystem.Modules.Cash.Application.DTOs;
using JewerlySystem.Modules.Cash.Domain;

namespace JewerlySystem.Modules.Cash.Application.DTOs;

public static class CashMappingExtensions
{
    public static readonly Expression<Func<DailyRecord, DailyRecordDto>> DailyRecordDtoProjection = r => new DailyRecordDto(
        r.Id,
        r.Date,
        r.Number,
        r.Reference,
        r.Description,
        r.Value,
        r.RecordType,
        r.PaymentMethod
    );

    public static DailyRecordDto ToDto(this DailyRecord record) => new(
        record.Id,
        record.Date,
        record.Number,
        record.Reference,
        record.Description,
        record.Value,
        record.RecordType,
        record.PaymentMethod
    );

    public static ConsolidatedRecordDto ToDto(this ConsolidatedRecord consolidated) => new(
        consolidated.Date,
        consolidated.TotalIngresos,
        consolidated.TotalEgresos,
        consolidated.Balance
    );

    public static IQueryable<DailyRecordDto> ProjectToDto(this IQueryable<DailyRecord> query) => query.Select(DailyRecordDtoProjection);
}
