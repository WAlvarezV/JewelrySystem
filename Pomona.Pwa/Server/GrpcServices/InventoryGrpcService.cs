using Google.Protobuf.WellKnownTypes;
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

        public override async Task<Response> RegisterItem(ItemProto item, ServerCallContext context)
        => await _service.RegisterItemAsync(item, CancellationToken.None);

        public override async Task<Response> DischargeItem(DischargeRequest discharge, ServerCallContext context)
        => await _service.DischargeItemAsync(discharge, CancellationToken.None);

        public override async Task<WatchesResponse> GetWatches(Pagination pagination, ServerCallContext context)
        => await _service.GetWatchesAsync(pagination, CancellationToken.None);

        public override async Task<ItemProto> GetItem(ItemRequest request, ServerCallContext context)
        => await _service.GetItemAsync(request, CancellationToken.None);

        public override async Task<JewelsResponse> GetJewels(Pagination pagination, ServerCallContext context)
        => await _service.GetJewelsAsync(pagination, CancellationToken.None);

        public override async Task<ConsolidatedResponse> GetConsolidated(Empty empty, ServerCallContext context)
        => await _service.GetConsolidatedAsync(empty, CancellationToken.None);
    }
}
