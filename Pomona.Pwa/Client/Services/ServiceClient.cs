using Grpc.Net.Client;
using static Pomona.Protos.ContractSrv;
using static Pomona.Protos.InventorySrv;

namespace Pomona.Pwa.Client.Services
{
    public class ServiceClient : IServiceClient
    {
        private InventorySrvClient InventoryClient { get; set; }
        private ContractSrvClient ContractClient { get; set; }


        public ServiceClient(GrpcChannel Channel)
        {
            InventoryClient = new InventorySrvClient(Channel);
            ContractClient = new ContractSrvClient(Channel);
        }


        public InventorySrvClient Inventory() => InventoryClient;
        public ContractSrvClient Contract() => ContractClient;
    }
}
