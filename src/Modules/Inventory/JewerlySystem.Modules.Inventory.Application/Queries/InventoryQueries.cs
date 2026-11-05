using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Inventory.Application.DTOs;
using JewerlySystem.Modules.Inventory.Domain;

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

public record GetItemByReferenceQuery(int Reference) : IQuery<ItemDetailDto?>;

public class GetItemByReferenceQueryHandler : IQueryHandler<GetItemByReferenceQuery, ItemDetailDto?>
{
    private readonly IInventoryRepository _repository;

    public GetItemByReferenceQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ItemDetailDto?>> Handle(GetItemByReferenceQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByReferenceAsync(request.Reference, cancellationToken);
        if (item == null)
        {
            return Result.Success<ItemDetailDto?>(null);
        }

        var types = await _repository.GetItemTypesAsync(cancellationToken);
        var typeName = types.FirstOrDefault(t => t.Id == item.ItemTypeId)?.Name ?? "Artículo";

        var jewel = await _repository.GetJewelByItemIdAsync(item.Id, cancellationToken);
        var watch = await _repository.GetWatchByItemIdAsync(item.Id, cancellationToken);

        var detail = new ItemDetailDto(
            item.Id,
            item.Reference,
            item.CostValue,
            item.DateOfEntry,
            item.DateOfSale,
            item.SaleValue,
            item.Description,
            item.Active,
            item.ItemTypeId,
            typeName,
            item.ProviderId,
            jewel?.ToDto(),
            watch?.ToDto(),
            item.ImageUrl,
            item.ThumbnailUrl
        );

        return Result.Success<ItemDetailDto?>(detail);
    }
}

public record GetConsolidatedInventoryQuery : IQuery<InventoryConsolidatedDto>;

public class GetConsolidatedInventoryQueryHandler : IQueryHandler<GetConsolidatedInventoryQuery, InventoryConsolidatedDto>
{
    private readonly IInventoryRepository _repository;

    public GetConsolidatedInventoryQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<InventoryConsolidatedDto>> Handle(GetConsolidatedInventoryQuery request, CancellationToken cancellationToken)
    {
        var activeItems = await _repository.ListItemsAsync(activeOnly: true, ct: cancellationToken);
        var types = await _repository.GetItemTypesAsync(cancellationToken);
        var brands = await _repository.GetBrandsAsync(cancellationToken);
        var allJewels = await _repository.ListJewelryAsync(cancellationToken);
        var allWatches = await _repository.ListWatchesAsync(cancellationToken);

        var jewelItems = activeItems.Where(i => i.ItemTypeId != 5).ToList(); // 5 is watch/relojes
        var watchItems = activeItems.Where(i => i.ItemTypeId == 5).ToList();

        var jewelSummary = new List<JewelSummaryDto>();
        foreach (var grp in jewelItems.GroupBy(i => i.ItemTypeId))
        {
            var tName = types.FirstOrDefault(t => t.Id == grp.Key)?.Name ?? $"Tipo {grp.Key}";
            var ids = grp.Select(x => x.Id).ToHashSet();
            var relatedJewels = allJewels.Where(j => ids.Contains(j.ItemId)).ToList();

            var qty = grp.Count();
            var totalWeight = relatedJewels.Sum(j => j.Weight);
            var totalCost = grp.Sum(x => (long)x.CostValue);
            var totalSale = grp.Sum(x => (long)(x.SaleValue ?? (int)(relatedJewels.FirstOrDefault(j => j.ItemId == x.Id)?.GramValue * relatedJewels.FirstOrDefault(j => j.ItemId == x.Id)?.Weight ?? x.CostValue * 1.3)));

            jewelSummary.Add(new JewelSummaryDto(tName, qty, totalWeight, totalSale, totalCost, $"jewel_{grp.Key}"));
        }

        var watchItemsWithDetail = watchItems.Select(item =>
        {
            var watch = allWatches.FirstOrDefault(w => w.ItemId == item.Id);
            var brandName = watch != null ? brands.FirstOrDefault(b => b.Id == watch.BrandId)?.Name ?? "Marca General" : "Reloj";
            var gender = watch?.GenderType.ToString() ?? "Unisex";
            var cost = (long)item.CostValue;
            var sale = (long)(item.SaleValue ?? (long)(item.CostValue * 1.3));
            return new { item, brandName, gender, cost, sale };
        }).ToList();

        var watchSummary = new List<WatchSummaryDto>();
        foreach (var grp in watchItemsWithDetail.GroupBy(w => new { w.brandName, w.gender }))
        {
            var brand = grp.Key.brandName;
            var gender = grp.Key.gender;
            var qty = grp.Count();
            var totalCost = grp.Sum(x => x.cost);
            var totalSale = grp.Sum(x => x.sale);

            watchSummary.Add(new WatchSummaryDto(brand, gender, qty, totalSale, totalCost, $"watch_{brand}_{gender}"));
        }

        return Result.Success(new InventoryConsolidatedDto(jewelSummary, watchSummary));
    }
}

public record GetInventoryCatalogQuery : IQuery<InventoryCatalogDto>;

public class GetInventoryCatalogQueryHandler : IQueryHandler<GetInventoryCatalogQuery, InventoryCatalogDto>
{
    private readonly IInventoryRepository _repository;

    public GetInventoryCatalogQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<InventoryCatalogDto>> Handle(GetInventoryCatalogQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.ListItemsAsync(ct: cancellationToken);
        var itemTypes = await _repository.GetItemTypesAsync(cancellationToken);
        var brands = await _repository.GetBrandsAsync(cancellationToken);
        var jewelry = await _repository.ListJewelryAsync(cancellationToken);
        var watches = await _repository.ListWatchesAsync(cancellationToken);

        var jewelMap = jewelry.ToDictionary(j => j.ItemId);
        var watchMap = watches.ToDictionary(w => w.ItemId);
        var typeMap = itemTypes.ToDictionary(t => t.Id, t => t.Name);
        var brandMap = brands.ToDictionary(b => b.Id, b => b.Name);

        var jewelList = new List<JewelListItemDto>();
        var watchList = new List<WatchListItemDto>();

        foreach (var item in items)
        {
            if (item.ItemTypeId == 5) // Watch
            {
                watchMap.TryGetValue(item.Id, out var w);
                var brandId = w?.BrandId ?? 0;
                brandMap.TryGetValue(brandId, out var brandName);

                watchList.Add(new WatchListItemDto(
                    item.Id,
                    item.Reference,
                    item.CostValue,
                    item.DateOfEntry,
                    item.DateOfSale,
                    item.SaleValue,
                    item.Description,
                    item.Active,
                    item.ItemTypeId,
                    item.ProviderId,
                    brandId,
                    brandName ?? "General",
                    w?.GenderType ?? GenderType.Unisex,
                    w?.CaseNumber ?? "-",
                    w?.Model ?? "",
                    item.ImageUrl
                ));
            }
            else // Jewelry
            {
                jewelMap.TryGetValue(item.Id, out var j);
                typeMap.TryGetValue(item.ItemTypeId, out var typeName);

                jewelList.Add(new JewelListItemDto(
                    item.Id,
                    item.Reference,
                    item.CostValue,
                    item.DateOfEntry,
                    item.DateOfSale,
                    item.SaleValue,
                    item.Description,
                    item.Active,
                    item.ItemTypeId,
                    typeName ?? "Joya",
                    item.ProviderId,
                    j?.Weight ?? 0,
                    j?.GramValue ?? 0,
                    j?.Length,
                    item.ImageUrl
                ));
            }
        }

        var typesDtos = itemTypes.Select(t => t.ToDto()).ToList();
        var brandDtos = brands.Select(b => b.ToDto()).ToList();

        return Result.Success(new InventoryCatalogDto(jewelList, watchList, typesDtos, brandDtos));
    }
}
