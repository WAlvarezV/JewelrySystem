using Microsoft.AspNetCore.Components;
using Pomona.Protos.Inventory;
using Pomona.Pwa.Client.Custom;
using System;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Cash
{
    public partial class DailyBase : CustomComponentBase
    {
        public ConsolidatedResponse Consolidated = new ConsolidatedResponse();
        [Parameter] public int ItemTypeId { get; set; }
        public string Tittle { get; set; }

        protected override async Task OnInitializedAsync()
        {
            base.OnInitialized();
            await GetConsolidated().ConfigureAwait(false);
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            await GetConsolidated().ConfigureAwait(false);
        }


        protected async Task GetConsolidated()
        {
            try
            {
                Consolidated = await Clients.Inventory().GetConsolidatedAsync(Empty);
            }
            catch (Exception ex)
            {
                var error = $"GetConsolidatedAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                Console.WriteLine(error);
            }
        }
    }
}