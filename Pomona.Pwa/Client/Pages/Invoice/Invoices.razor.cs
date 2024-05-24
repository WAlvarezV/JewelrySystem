using Pomona.Protos.Common;
using Pomona.Protos.Contract;
using Pomona.Pwa.Client.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Invoice
{
    public class InvoicesBase : CustomComponentBase
    {
        public IEnumerable<ContractProto> Contracts { get; set; } = Enumerable.Empty<ContractProto>();

        protected override async Task OnInitializedAsync() => await GetContracts();

        protected async Task GetContracts()
        {
            try
            {
                Contracts = (await Clients.Contract().GetContractsAsync(Empty)).ItemsList;
            }
            catch (Exception ex)
            {
                var error = $"GetContractsAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                Console.WriteLine(error);
            }
        }

        protected async Task FilteredSearch()
        {
            Pagination.Page = 1;
            Pagination.Records = 10;
            await GetContracts();
        }

        protected async Task SelectedPage(Pagination pagination)
        {
            Pagination = pagination;
            await GetContracts();
        }

        protected async Task ClearFilter()
        {
            ClearPaginationFilter();
            await GetContracts();
        }
    }
}
