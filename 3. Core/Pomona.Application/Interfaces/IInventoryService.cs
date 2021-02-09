using Pomona.Protos.Common;
using Pomona.Protos.Inventory;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Interfaces
{
    internal interface IInventoryService
    {
        Task<ItemResponse> RegisterItemAsync(ItemProto item, CancellationToken cancelToken);
        Task<WatchesResponse> GetWatchesAsync(Pagination pagination, CancellationToken cancelToken);
    }
}
