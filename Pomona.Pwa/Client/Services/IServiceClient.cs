using static Pomona.Protos.ContractSrv;
using static Pomona.Protos.InventorySrv;

namespace Pomona.Pwa.Client.Services
{
    public interface IServiceClient
    {
        InventorySrvClient Inventory();
        ContractSrvClient Contract();
    }
}
