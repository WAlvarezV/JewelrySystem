using Grpc.Core;
using Pomona.Application.Interfaces;
using Pomona.Protos.Common;
using Pomona.Protos.Inventory;
using System.Threading;
using System.Threading.Tasks;
using static Pomona.Protos.Inventory.InventorySrv;

namespace Pomona.Pwa.Server.GrpcServices
{
    internal class InventoryGrpcService : InventorySrvBase
    {
        private readonly IInventoryService _service;
        public InventoryGrpcService(IInventoryService service) => _service = service;

        public override async Task<ItemResponse> RegisterItem(ItemProto item, ServerCallContext context)
        => await _service.RegisterItemAsync(item, CancellationToken.None);

        public override async Task<WatchesResponse> GetWatches(Pagination pagination, ServerCallContext context)
       => await _service.GetWatchesAsync(pagination, CancellationToken.None);
    }
}
