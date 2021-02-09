using static Pomona.Protos.Contract.ContractSrv;
using static Pomona.Protos.Inventory.InventorySrv;
using static Pomona.Protos.Parametric.ParametricSrv;

namespace Pomona.Pwa.Client.Services
{
    public interface IServiceClient
    {
        InventorySrvClient Inventory();
        ContractSrvClient Contract();
        ParametricSrvClient Parametric();
    }
}
