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
    int? ProviderId);

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
    int BrandId);

public record BrandDto(
    int Id,
    string Name);

public record ItemTypeDto(
    int Id,
    string Name);

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
    WatchDto? Watch);

public record JewelSummaryDto(
    string Type,
    int Quantity,
    double Weight,
    long SaleValue,
    long CostValue);

public record WatchSummaryDto(
    string Type,
    string Other,
    int Quantity,
    long SaleValue,
    long CostValue);

public record InventoryConsolidatedDto(
    List<JewelSummaryDto> Jewelry,
    List<WatchSummaryDto> Watches);
