using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Inventory.Application.DTOs;

namespace JewerlySystem.Modules.Inventory.Application.Queries;

public record GetItemByIdQuery(int Id) : IQuery<ItemDto>;

public class GetItemByIdQueryHandler : IQueryHandler<GetItemByIdQuery, ItemDto>
{
    private readonly IInventoryRepository _repository;

    public GetItemByIdQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ItemDto>> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByIdAsync(request.Id, cancellationToken);
        if (item == null)
        {
            return Result.Failure<ItemDto>(Error.NotFound("Items.NotFound", $"Artículo con ID {request.Id} no encontrado."));
        }

        return Result.Success(item.ToDto());
    }
}

public record GetItemsListQuery(bool? ActiveOnly = null, int? ItemTypeId = null) : IQuery<List<ItemDto>>;

public class GetItemsListQueryHandler : IQueryHandler<GetItemsListQuery, List<ItemDto>>
{
    private readonly IInventoryRepository _repository;

    public GetItemsListQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<ItemDto>>> Handle(GetItemsListQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.ListItemsAsync(request.ActiveOnly, request.ItemTypeId, cancellationToken);
        var dtos = items.Select(i => i.ToDto()).ToList();

        return Result.Success(dtos);
    }
}

public record GetBrandsQuery : IQuery<List<BrandDto>>;

public class GetBrandsQueryHandler : IQueryHandler<GetBrandsQuery, List<BrandDto>>
{
    private readonly IInventoryRepository _repository;

    public GetBrandsQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<BrandDto>>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        var brands = await _repository.GetBrandsAsync(cancellationToken);
        var dtos = brands.Select(b => b.ToDto()).ToList();

        return Result.Success(dtos);
    }
}

public record GetItemTypesQuery : IQuery<List<ItemTypeDto>>;

public class GetItemTypesQueryHandler : IQueryHandler<GetItemTypesQuery, List<ItemTypeDto>>
{
    private readonly IInventoryRepository _repository;

    public GetItemTypesQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<ItemTypeDto>>> Handle(GetItemTypesQuery request, CancellationToken cancellationToken)
    {
        var types = await _repository.GetItemTypesAsync(cancellationToken);
        var dtos = types.Select(t => t.ToDto()).ToList();

        return Result.Success(dtos);
    }
}
