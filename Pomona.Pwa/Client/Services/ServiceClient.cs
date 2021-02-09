using Grpc.Net.Client;
using static Pomona.Protos.Contract.ContractSrv;
using static Pomona.Protos.Inventory.InventorySrv;
using static Pomona.Protos.Parametric.ParametricSrv;

namespace Pomona.Pwa.Client.Services
{
    public class ServiceClient : IServiceClient
    {
        private InventorySrvClient InventoryClient { get; set; }
        private ContractSrvClient ContractClient { get; set; }
        private ParametricSrvClient ParametricClient { get; set; }


        public ServiceClient(GrpcChannel Channel)
        {
            InventoryClient = new InventorySrvClient(Channel);
            ContractClient = new ContractSrvClient(Channel);
            ParametricClient = new ParametricSrvClient(Channel);
        }

        public InventorySrvClient Inventory() => InventoryClient;
        public ContractSrvClient Contract() => ContractClient;
        public ParametricSrvClient Parametric() => ParametricClient;
    }
}
