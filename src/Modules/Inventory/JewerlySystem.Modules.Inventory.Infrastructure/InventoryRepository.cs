using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Inventory.Application;
using JewerlySystem.Modules.Inventory.Application.DTOs;
using JewerlySystem.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Inventory.Infrastructure;

public class InventoryRepository : IInventoryRepository
{
    private static readonly SemaphoreSlim _referenceLock = new(1, 1);
    private const long ReferenceAllocationLockId = 7492812938471;
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
        var activeItem = await _dbContext.Items
            .Where(i => i.Reference == reference && i.Active && i.DateOfSale == null)
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync(ct);

        if (activeItem != null)
        {
            return activeItem;
        }

        return await _dbContext.Items
            .Where(i => i.Reference == reference)
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync(ct);
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

    public async Task<IReadOnlyList<UserActivityLog>> GetItemActivityLogsAsync(string reference, CancellationToken ct = default)
    {
        return await _dbContext.UserActivityLogs
            .AsNoTracking()
            .Where(x => x.EntityName == "Item" && x.EntityReference == reference)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetNextItemReferenceAsync(CancellationToken ct = default)
    {
        var occupiedRefs = await _dbContext.Items
            .Where(i => i.Active && i.DateOfSale == null && i.Reference > 0)
            .Select(i => i.Reference)
            .Distinct()
            .ToListAsync(ct);

        var occupiedSet = occupiedRefs.ToHashSet();
        int candidate = 1;
        while (occupiedSet.Contains(candidate))
        {
            candidate++;
        }

        return candidate;
    }

    public async Task<CreateItemResponse> CreateJewelItemAtomicAsync(Item item, Jewel jewel, CancellationToken ct = default)
    {
        await _referenceLock.WaitAsync(ct);
        try
        {
            var isRelational = _dbContext.Database.IsRelational();
            await using var tx = isRelational ? await _dbContext.Database.BeginTransactionAsync(ct) : null;

            if (isRelational && _dbContext.Database.IsNpgsql())
            {
                await _dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({ReferenceAllocationLockId});", ct);
            }

            var nextRef = await GetNextItemReferenceAsync(ct);
            item.Reference = nextRef;

            await _dbContext.Items.AddAsync(item, ct);
            await _dbContext.SaveChangesAsync(ct);

            jewel.ItemId = item.Id;
            await _dbContext.Jewelry.AddAsync(jewel, ct);
            await _dbContext.SaveChangesAsync(ct);

            if (tx != null)
            {
                await tx.CommitAsync(ct);
            }

            return new CreateItemResponse(item.Id, nextRef);
        }
        finally
        {
            _referenceLock.Release();
        }
    }

    public async Task<CreateItemResponse> CreateWatchItemAtomicAsync(Item item, Watch watch, CancellationToken ct = default)
    {
        await _referenceLock.WaitAsync(ct);
        try
        {
            var isRelational = _dbContext.Database.IsRelational();
            await using var tx = isRelational ? await _dbContext.Database.BeginTransactionAsync(ct) : null;

            if (isRelational && _dbContext.Database.IsNpgsql())
            {
                await _dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({ReferenceAllocationLockId});", ct);
            }

            var nextRef = await GetNextItemReferenceAsync(ct);
            item.Reference = nextRef;

            await _dbContext.Items.AddAsync(item, ct);
            await _dbContext.SaveChangesAsync(ct);

            watch.ItemId = item.Id;
            await _dbContext.Watches.AddAsync(watch, ct);
            await _dbContext.SaveChangesAsync(ct);

            if (tx != null)
            {
                await tx.CommitAsync(ct);
            }

            return new CreateItemResponse(item.Id, nextRef);
        }
        finally
        {
            _referenceLock.Release();
        }
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
