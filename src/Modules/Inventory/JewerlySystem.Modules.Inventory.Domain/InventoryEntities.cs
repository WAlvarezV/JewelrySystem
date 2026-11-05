using JewerlySystem.Common.Domain;

namespace JewerlySystem.Modules.Inventory.Domain;

public class Brand : Entity<int>
{
    public string Name { get; set; } = string.Empty;

    public Brand() { }
    public Brand(int id, string name) : base(id)
    {
        Name = name;
    }
}

public class ItemType : Entity<int>
{
    public string Name { get; set; } = string.Empty;

    public ItemType() { }
    public ItemType(int id, string name) : base(id)
    {
        Name = name;
    }
}

public class Item : AuditableEntity<int>
{
    public int Reference { get; set; }
    public int CostValue { get; set; }
    public DateTime DateOfEntry { get; set; }
    public DateTime? DateOfSale { get; set; }
    public int? SaleValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public int ItemTypeId { get; set; }
    public int? ProviderId { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }

    public Item() { }

    public Item(
        int reference,
        int costValue,
        DateTime dateOfEntry,
        string description,
        int itemTypeId,
        int? providerId = null,
        string? imageUrl = null,
        string? thumbnailUrl = null)
    {
        Reference = reference;
        CostValue = costValue;
        DateOfEntry = dateOfEntry;
        Description = description ?? string.Empty;
        ItemTypeId = itemTypeId;
        ProviderId = providerId;
        ImageUrl = imageUrl;
        ThumbnailUrl = thumbnailUrl;
        Active = true;
    }

    public void Discharge(DateTime saleDate, int saleValue)
    {
        DateOfSale = saleDate;
        SaleValue = saleValue;
        Active = false;
    }
}

public class Jewel : Entity<int>
{
    public int ItemId { get; set; }
    public double Weight { get; set; }
    public int GramValue { get; set; }
    public int? Length { get; set; }

    public Jewel() { }

    public Jewel(int itemId, double weight, int gramValue, int? length = null)
    {
        ItemId = itemId;
        Weight = weight;
        GramValue = gramValue;
        Length = length;
    }
}

public class Watch : Entity<int>
{
    public int ItemId { get; set; }
    public GenderType GenderType { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int BrandId { get; set; }

    public Watch() { }

    public Watch(int itemId, GenderType genderType, string caseNumber, int brandId, string? model = null)
    {
        ItemId = itemId;
        GenderType = genderType;
        CaseNumber = caseNumber ?? string.Empty;
        BrandId = brandId;
        Model = model ?? string.Empty;
    }
}
