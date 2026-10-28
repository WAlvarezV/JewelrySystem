using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Cash.Application.DTOs;
using JewerlySystem.Modules.Cash.Domain;

namespace JewerlySystem.Modules.Cash.Application.Queries;

public record GetDailyRecordByIdQuery(int Id) : IQuery<DailyRecordDto>;

public class GetDailyRecordByIdQueryHandler : IQueryHandler<GetDailyRecordByIdQuery, DailyRecordDto>
{
    private readonly ICashRepository _repository;

    public GetDailyRecordByIdQueryHandler(ICashRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DailyRecordDto>> Handle(GetDailyRecordByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (record == null)
        {
            return Result.Failure<DailyRecordDto>(Error.NotFound("Cash.NotFound", $"Registro de caja con ID {request.Id} no encontrado."));
        }

        return Result.Success(record.ToDto());
    }
}

public record GetDailyRecordsListQuery(DateTime? Date = null, RecordType? Type = null) : IQuery<List<DailyRecordDto>>;

public class GetDailyRecordsListQueryHandler : IQueryHandler<GetDailyRecordsListQuery, List<DailyRecordDto>>
{
    private readonly ICashRepository _repository;

    public GetDailyRecordsListQueryHandler(ICashRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<DailyRecordDto>>> Handle(GetDailyRecordsListQuery request, CancellationToken cancellationToken)
    {
        var records = await _repository.ListAsync(request.Date, request.Type, cancellationToken);
        var dtos = records.Select(r => r.ToDto()).ToList();

        return Result.Success(dtos);
    }
}

public record GetCashConsolidatedQuery(DateTime Date) : IQuery<ConsolidatedRecordDto>;

public class GetCashConsolidatedQueryHandler : IQueryHandler<GetCashConsolidatedQuery, ConsolidatedRecordDto>
{
    private readonly ICashRepository _repository;

    public GetCashConsolidatedQueryHandler(ICashRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ConsolidatedRecordDto>> Handle(GetCashConsolidatedQuery request, CancellationToken cancellationToken)
    {
        var consolidated = await _repository.GetConsolidatedAsync(request.Date, cancellationToken);
        return Result.Success(consolidated.ToDto());
    }
}

public record GetCashConsolidatedRangeQuery(DateTime StartDate, DateTime EndDate) : IQuery<List<CashRangeConsolidatedItemDto>>;

public class GetCashConsolidatedRangeQueryHandler : IQueryHandler<GetCashConsolidatedRangeQuery, List<CashRangeConsolidatedItemDto>>
{
    private readonly ICashRepository _repository;

    public GetCashConsolidatedRangeQueryHandler(ICashRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<CashRangeConsolidatedItemDto>>> Handle(GetCashConsolidatedRangeQuery request, CancellationToken cancellationToken)
    {
        var allRecords = await _repository.ListAsync(ct: cancellationToken);
        var inRange = allRecords
            .Where(r => r.Date.Date >= request.StartDate.Date && r.Date.Date <= request.EndDate.Date)
            .GroupBy(r => r.Date.Date)
            .OrderBy(g => g.Key);

        var list = new List<CashRangeConsolidatedItemDto>();
        foreach (var grp in inRange)
        {
            var date = grp.Key;
            var cashIn = grp.Where(r => r.RecordType == RecordType.INGRESO && r.PaymentMethod == PaymentMethod.EFECTIVO).Sum(r => r.Value);
            var othersIn = grp.Where(r => r.RecordType == RecordType.INGRESO && r.PaymentMethod != PaymentMethod.EFECTIVO).Sum(r => r.Value);
            var totalIn = cashIn + othersIn;
            var cashOut = grp.Where(r => r.RecordType == RecordType.EGRESO).Sum(r => r.Value);
            var totalNet = totalIn - cashOut;
            var cashBalance = cashIn - cashOut;

            list.Add(new CashRangeConsolidatedItemDto(date, cashIn, othersIn, totalIn, cashOut, totalNet, cashBalance));
        }

        return Result.Success(list);
    }
}
