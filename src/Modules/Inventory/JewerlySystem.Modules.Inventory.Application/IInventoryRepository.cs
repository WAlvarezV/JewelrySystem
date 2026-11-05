using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Inventory.Application.DTOs;
using JewerlySystem.Modules.Inventory.Domain;

namespace JewerlySystem.Modules.Inventory.Application;

public interface IInventoryRepository
{
    Task<Item?> GetItemByIdAsync(int id, CancellationToken ct = default);
    Task<Item?> GetItemByReferenceAsync(int reference, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> ListItemsAsync(bool? activeOnly = null, int? itemTypeId = null, CancellationToken ct = default);
    Task<Jewel?> GetJewelByItemIdAsync(int itemId, CancellationToken ct = default);
    Task<Watch?> GetWatchByItemIdAsync(int itemId, CancellationToken ct = default);
    Task<IReadOnlyList<Jewel>> ListJewelryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Watch>> ListWatchesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Brand>> GetBrandsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ItemType>> GetItemTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UserActivityLog>> GetItemActivityLogsAsync(string reference, CancellationToken ct = default);
    Task<int> GetNextItemReferenceAsync(CancellationToken ct = default);
    Task<CreateItemResponse> CreateJewelItemAtomicAsync(Item item, Jewel jewel, CancellationToken ct = default);
    Task<CreateItemResponse> CreateWatchItemAtomicAsync(Item item, Watch watch, CancellationToken ct = default);
    Task AddItemAsync(Item item, CancellationToken ct = default);
    Task AddJewelAsync(Jewel jewel, CancellationToken ct = default);
    Task AddWatchAsync(Watch watch, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
