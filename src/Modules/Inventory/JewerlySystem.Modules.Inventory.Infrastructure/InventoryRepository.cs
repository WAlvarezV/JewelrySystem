using JewerlySystem.Modules.Inventory.Application;
using JewerlySystem.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Inventory.Infrastructure;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _dbContext;

    public InventoryRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Item?> GetItemByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<Item?> GetItemByReferenceAsync(int reference, CancellationToken ct = default)
    {
        return await _dbContext.Items.FirstOrDefaultAsync(i => i.Reference == reference, ct);
    }

    public async Task<IReadOnlyList<Item>> ListItemsAsync(bool? activeOnly = null, int? itemTypeId = null, CancellationToken ct = default)
    {
        var query = _dbContext.Items.AsNoTracking().AsQueryable();

        if (activeOnly.HasValue)
        {
            query = query.Where(i => i.Active == activeOnly.Value);
        }

        if (itemTypeId.HasValue)
        {
            query = query.Where(i => i.ItemTypeId == itemTypeId.Value);
        }

        return await query.OrderByDescending(i => i.DateOfEntry).ToListAsync(ct);
    }

    public async Task<Jewel?> GetJewelByItemIdAsync(int itemId, CancellationToken ct = default)
    {
        return await _dbContext.Jewelry.FirstOrDefaultAsync(j => j.ItemId == itemId, ct);
    }

    public async Task<Watch?> GetWatchByItemIdAsync(int itemId, CancellationToken ct = default)
    {
        return await _dbContext.Watches.FirstOrDefaultAsync(w => w.ItemId == itemId, ct);
    }

    public async Task<IReadOnlyList<Jewel>> ListJewelryAsync(CancellationToken ct = default)
    {
        return await _dbContext.Jewelry.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Watch>> ListWatchesAsync(CancellationToken ct = default)
    {
        return await _dbContext.Watches.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Brand>> GetBrandsAsync(CancellationToken ct = default)
    {
        return await _dbContext.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ItemType>> GetItemTypesAsync(CancellationToken ct = default)
    {
        return await _dbContext.ItemTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
    }

    public async Task<int> GetNextItemReferenceAsync(CancellationToken ct = default)
    {
        var maxRef = await _dbContext.Items.MaxAsync(i => (int?)i.Reference, ct) ?? 0;
        return maxRef + 1;
    }

    public async Task AddItemAsync(Item item, CancellationToken ct = default)
    {
        await _dbContext.Items.AddAsync(item, ct);
    }

    public async Task AddJewelAsync(Jewel jewel, CancellationToken ct = default)
    {
        await _dbContext.Jewelry.AddAsync(jewel, ct);
    }

    public async Task AddWatchAsync(Watch watch, CancellationToken ct = default)
    {
        await _dbContext.Watches.AddAsync(watch, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
