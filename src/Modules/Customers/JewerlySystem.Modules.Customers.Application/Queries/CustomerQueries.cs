using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Customers.Application.DTOs;
using JewerlySystem.Modules.Customers.Domain;

namespace JewerlySystem.Modules.Customers.Application.Queries;

public record GetCustomerByIdQuery(int Id) : IQuery<PersonDto>;

public class GetCustomerByIdQueryHandler : IQueryHandler<GetCustomerByIdQuery, PersonDto>
{
    private readonly ICustomerRepository _repository;

    public GetCustomerByIdQueryHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PersonDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var person = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (person == null)
        {
            return Result.Failure<PersonDto>(Error.NotFound("Customers.NotFound", $"Cliente con ID {request.Id} no encontrado."));
        }

        return Result.Success(person.ToDto());
    }
}

public record GetCustomersListQuery(PersonType? Type = null, string? SearchTerm = null) : IQuery<List<PersonDto>>;

public class GetCustomersListQueryHandler : IQueryHandler<GetCustomersListQuery, List<PersonDto>>
{
    private readonly ICustomerRepository _repository;

    public GetCustomersListQueryHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<PersonDto>>> Handle(GetCustomersListQuery request, CancellationToken cancellationToken)
    {
        var persons = await _repository.ListAsync(request.Type, request.SearchTerm, cancellationToken);
        var dtos = persons.Select(p => p.ToDto()).ToList();

        return Result.Success(dtos);
    }
}

public record GetIdentificationTypesQuery : IQuery<List<IdentificationTypeDto>>;

public class GetIdentificationTypesQueryHandler : IQueryHandler<GetIdentificationTypesQuery, List<IdentificationTypeDto>>
{
    private readonly ICustomerRepository _repository;

    public GetIdentificationTypesQueryHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<IdentificationTypeDto>>> Handle(GetIdentificationTypesQuery request, CancellationToken cancellationToken)
    {
        var types = await _repository.GetIdentificationTypesAsync(cancellationToken);
        var dtos = types.Select(t => t.ToDto()).ToList();

        return Result.Success(dtos);
    }
}
