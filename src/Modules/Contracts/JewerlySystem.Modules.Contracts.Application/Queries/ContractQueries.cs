using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Contracts.Application.DTOs;
using JewerlySystem.Modules.Contracts.Domain;

namespace JewerlySystem.Modules.Contracts.Application.Queries;

public record GetContractByIdQuery(int Id) : IQuery<ContractDto>;

public class GetContractByIdQueryHandler : IQueryHandler<GetContractByIdQuery, ContractDto>
{
    private readonly IContractRepository _repository;

    public GetContractByIdQueryHandler(IContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ContractDto>> Handle(GetContractByIdQuery request, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (contract == null)
        {
            return Result.Failure<ContractDto>(Error.NotFound("Contracts.NotFound", $"Contrato con ID {request.Id} no encontrado."));
        }

        return Result.Success(contract.ToDto());
    }
}

public record GetContractsListQuery(int? PersonId = null, ContractState? State = null) : IQuery<List<ContractDto>>;

public class GetContractsListQueryHandler : IQueryHandler<GetContractsListQuery, List<ContractDto>>
{
    private readonly IContractRepository _repository;

    public GetContractsListQueryHandler(IContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<ContractDto>>> Handle(GetContractsListQuery request, CancellationToken cancellationToken)
    {
        var contracts = await _repository.ListAsync(request.PersonId, request.State, cancellationToken);
        var dtos = contracts.Select(c => c.ToDto()).ToList();

        return Result.Success(dtos);
    }
}
