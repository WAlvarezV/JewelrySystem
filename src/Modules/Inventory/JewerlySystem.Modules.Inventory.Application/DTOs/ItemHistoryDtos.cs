namespace JewerlySystem.Modules.Inventory.Application.DTOs;

public record ItemHistoryEventDto(
    long Id,
    DateTime Timestamp,
    string Operation,
    string PerformedBy,
    string Description,
    string Category,
    string TagColor,
    string Icon);

public record ItemHistoryResponseDto(
    int Reference,
    string Description,
    string ItemType,
    bool Active,
    int CostValue,
    double? Weight,
    int? GramValue,
    string? CaseNumber,
    string? BrandName,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? LastModifiedAt,
    string? LastModifiedBy,
    DateTime? DateOfSale,
    int? SaleValue,
    IReadOnlyList<ItemHistoryEventDto> Events);
