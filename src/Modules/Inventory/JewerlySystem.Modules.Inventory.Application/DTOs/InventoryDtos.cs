using JewerlySystem.Modules.Inventory.Domain;

namespace JewerlySystem.Modules.Inventory.Application.DTOs;

public record ItemDto(
    int Id,
    int Reference,
    int CostValue,
    DateTime DateOfEntry,
    DateTime? DateOfSale,
    int? SaleValue,
    string Description,
    bool Active,
    int ItemTypeId,
    int? ProviderId,
    string? ImageUrl = null,
    string? ThumbnailUrl = null);

public record JewelDto(
    int Id,
    int ItemId,
    double Weight,
    int GramValue,
    int? Length);

public record WatchDto(
    int Id,
    int ItemId,
    GenderType GenderType,
    string CaseNumber,
    string Model,
    int BrandId);

public record BrandDto(
    int Id,
    string Name);

public record ItemTypeDto(
    int Id,
    string Name);

public record CreateItemResponse(int Id, int Reference)
{
    public string FormattedReference => Reference.ToString("D4");
}

public record ItemDetailDto(
    int Id,
    int Reference,
    int CostValue,
    DateTime DateOfEntry,
    DateTime? DateOfSale,
    int? SaleValue,
    string Description,
    bool Active,
    int ItemTypeId,
    string ItemTypeName,
    int? ProviderId,
    JewelDto? Jewel,
    WatchDto? Watch,
    string? ImageUrl = null,
    string? ThumbnailUrl = null);

public record JewelSummaryDto(
    string Type,
    int Quantity,
    double Weight,
    long SaleValue,
    long CostValue,
    string Key = "");

public record WatchSummaryDto(
    string Type,
    string Other,
    int Quantity,
    long SaleValue,
    long CostValue,
    string Key = "");

public record InventoryConsolidatedDto(
    List<JewelSummaryDto> Jewelry,
    List<WatchSummaryDto> Watches);

public record JewelListItemDto(
    int Id,
    int Reference,
    int CostValue,
    DateTime DateOfEntry,
    DateTime? DateOfSale,
    int? SaleValue,
    string Description,
    bool Active,
    int ItemTypeId,
    string ItemTypeName,
    int? ProviderId,
    double Weight,
    int GramValue,
    int? Length,
    string? ImageUrl = null)
{
    public string SearchIndex { get; init; } = string.Empty;
}

public record WatchListItemDto(
    int Id,
    int Reference,
    int CostValue,
    DateTime DateOfEntry,
    DateTime? DateOfSale,
    int? SaleValue,
    string Description,
    bool Active,
    int ItemTypeId,
    int? ProviderId,
    int BrandId,
    string BrandName,
    GenderType GenderType,
    string CaseNumber,
    string Model,
    string? ImageUrl = null)
{
    public string SearchIndex { get; init; } = string.Empty;
}

public record InventoryCatalogDto(
    List<JewelListItemDto> Jewelry,
    List<WatchListItemDto> Watches,
    List<ItemTypeDto> ItemTypes,
    List<BrandDto> Brands);
