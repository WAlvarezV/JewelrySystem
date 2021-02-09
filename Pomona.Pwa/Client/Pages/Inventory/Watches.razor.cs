using Pomona.Protos.Common;
using Pomona.Protos.Inventory;
using Pomona.Pwa.Client.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Inventory
{
    public partial class WatchesBase : CustomComponentBase
    {
        public IEnumerable<WatchProto> Watches { get; set; } = Enumerable.Empty<WatchProto>();

        protected override async Task OnInitializedAsync()
        {
            base.OnInitialized();
            await GetWatches().ConfigureAwait(false);
        }

        protected async Task GetWatches()
        {
            try
            {
                Watches = (await Clients.Inventory().GetWatchesAsync(Pagination)).Watches;
            }
            catch (Exception ex)
            {
                var error = $"GetWatchesAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                Console.WriteLine(error);
            }
        }

        protected async Task FilteredSearch()
        {
            Pagination.Page = 1;
            Pagination.Records = 10;
            await GetWatches().ConfigureAwait(true);
        }

        protected async Task SelectedPage(Pagination pagination)
        {
            Pagination = pagination;
            await GetWatches().ConfigureAwait(true);
        }

        protected async Task ClearFilter()
        {
            ClearPaginationFilter();
            await GetWatches().ConfigureAwait(false);
        }
    }
}
