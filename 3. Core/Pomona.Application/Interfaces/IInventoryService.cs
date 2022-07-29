using Google.Protobuf.WellKnownTypes;
using Pomona.Protos.Common;
using Pomona.Protos.Inventory;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Interfaces
{
    internal interface IInventoryService
    {
        Task<Response> RegisterItemAsync(ItemProto item, CancellationToken cancelToken);
        Task<Response> DischargeItemAsync(DischargeRequest discharge, CancellationToken cancelToken);
        Task<ItemProto> GetItemAsync(ItemRequest request, CancellationToken cancelToken);
        Task<WatchesResponse> GetWatchesAsync(Pagination pagination, CancellationToken cancelToken);
        Task<JewelsResponse> GetJewelsAsync(Pagination pagination, CancellationToken cancelToken);
        Task<ConsolidatedResponse> GetConsolidatedAsync(Empty pagination, CancellationToken cancelToken);
    }
}
