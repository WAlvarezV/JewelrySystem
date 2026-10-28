using System.Linq;
using System.Linq.Expressions;
using JewerlySystem.Modules.Inventory.Application.DTOs;
using JewerlySystem.Modules.Inventory.Domain;

namespace JewerlySystem.Modules.Inventory.Application.DTOs;

public static class InventoryMappingExtensions
{
    public static readonly Expression<Func<Item, ItemDto>> ItemDtoProjection = i => new ItemDto(
        i.Id,
        i.Reference,
        i.CostValue,
        i.DateOfEntry,
        i.DateOfSale,
        i.SaleValue,
        i.Description,
        i.Active,
        i.ItemTypeId,
        i.ProviderId
    );

    public static readonly Expression<Func<Jewel, JewelDto>> JewelDtoProjection = j => new JewelDto(
        j.Id,
        j.ItemId,
        j.Weight,
        j.GramValue,
        j.Length
    );

    public static readonly Expression<Func<Watch, WatchDto>> WatchDtoProjection = w => new WatchDto(
        w.Id,
        w.ItemId,
        w.GenderType,
        w.CaseNumber,
        w.BrandId
    );

    public static readonly Expression<Func<Brand, BrandDto>> BrandDtoProjection = b => new BrandDto(
        b.Id,
        b.Name
    );

    public static readonly Expression<Func<ItemType, ItemTypeDto>> ItemTypeDtoProjection = t => new ItemTypeDto(
        t.Id,
        t.Name
    );

    public static ItemDto ToDto(this Item item) => new(
        item.Id,
        item.Reference,
        item.CostValue,
        item.DateOfEntry,
        item.DateOfSale,
        item.SaleValue,
        item.Description,
        item.Active,
        item.ItemTypeId,
        item.ProviderId
    );

    public static JewelDto ToDto(this Jewel jewel) => new(
        jewel.Id,
        jewel.ItemId,
        jewel.Weight,
        jewel.GramValue,
        jewel.Length
    );

    public static WatchDto ToDto(this Watch watch) => new(
        watch.Id,
        watch.ItemId,
        watch.GenderType,
        watch.CaseNumber,
        watch.BrandId
    );

    public static BrandDto ToDto(this Brand brand) => new(
        brand.Id,
        brand.Name
    );

    public static ItemTypeDto ToDto(this ItemType itemType) => new(
        itemType.Id,
        itemType.Name
    );

    public static IQueryable<ItemDto> ProjectToDto(this IQueryable<Item> query) => query.Select(ItemDtoProjection);
    public static IQueryable<JewelDto> ProjectToDto(this IQueryable<Jewel> query) => query.Select(JewelDtoProjection);
    public static IQueryable<WatchDto> ProjectToDto(this IQueryable<Watch> query) => query.Select(WatchDtoProjection);
    public static IQueryable<BrandDto> ProjectToDto(this IQueryable<Brand> query) => query.Select(BrandDtoProjection);
    public static IQueryable<ItemTypeDto> ProjectToDto(this IQueryable<ItemType> query) => query.Select(ItemTypeDtoProjection);
}
