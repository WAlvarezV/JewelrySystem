using Pomona.Protos;
using Pomona.Pwa.Client.Custom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Contract
{
    public partial class ContractsBase : CustomComponentBase
    {
        public IEnumerable<ContractProto> Contracts { get; set; } = Enumerable.Empty<ContractProto>();

        protected override async Task OnInitializedAsync()
        {
            //await SetToken();
            base.OnInitialized();
            await GetRoles().ConfigureAwait(false);
        }

        protected async Task GetRoles() =>
            Contracts = (await Clients.Contract().GetContractsAsync(Empty)).ItemsList;

    }
}
