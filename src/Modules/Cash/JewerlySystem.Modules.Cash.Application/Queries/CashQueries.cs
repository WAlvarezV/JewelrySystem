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
