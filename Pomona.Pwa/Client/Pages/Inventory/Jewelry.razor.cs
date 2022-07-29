using Microsoft.AspNetCore.Components;
using Pomona.Protos.Common;
using Pomona.Protos.Inventory;
using Pomona.Pwa.Client.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Inventory
{
    public partial class JewelryBase : CustomComponentBase
    {
        public IEnumerable<JewelProto> Jewels { get; set; } = Enumerable.Empty<JewelProto>();
        [Parameter] public int ItemTypeId { get; set; }
        public string Tittle { get; set; }

        protected override async Task OnInitializedAsync()
        {
            base.OnInitialized();
            SetTittle();
            await GetJewels().ConfigureAwait(false);
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            SetTittle();
            await GetJewels().ConfigureAwait(false);
        }


        protected async Task GetJewels()
        {
            try
            {
                Pagination.Filter.Type = ItemTypeId.ToString();
                Jewels = (await Clients.Inventory().GetJewelsAsync(Pagination)).Jewels;
            }
            catch (Exception ex)
            {
                var error = $"GetJewelsAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                Console.WriteLine(error);
            }
        }

        protected async Task FilteredSearch()
        {
            Pagination.Page = 1;
            Pagination.Records = 10;
            await GetJewels().ConfigureAwait(true);
        }

        protected async Task SelectedPage(Pagination pagination)
        {
            Pagination = pagination;
            await GetJewels().ConfigureAwait(true);
        }

        protected async Task ClearFilter()
        {
            ClearPaginationFilter();
            await GetJewels().ConfigureAwait(false);
        }

        private void SetTittle()
        {
            Tittle = ItemTypeId switch
            {
                1 => "Anillos",
                2 => "Aretes",
                3 => "Cadenas",
                4 => "Pulseras",
                6 => "Dijes",
                _ => "Joyas",
            };
        }
    }
}