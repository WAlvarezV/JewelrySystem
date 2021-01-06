using Grpc.Core;
using Pomona.Application.Interfaces;
using Pomona.Protos;
using System.Threading;
using System.Threading.Tasks;
using static Pomona.Protos.InventorySrv;

namespace Pomona.Pwa.Server.GrpcServices
{
    internal class InventoryGrpcService : InventorySrvBase
    {
        private readonly IInventoryService _service;
        public InventoryGrpcService(IInventoryService service) => _service = service;

        public override async Task<ItemResponse> RegisterItem(ItemProto item, ServerCallContext context)
        => await _service.RegisterItemAsync(item, CancellationToken.None);
    }
}
