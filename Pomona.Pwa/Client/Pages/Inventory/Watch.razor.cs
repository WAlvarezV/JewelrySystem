using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Pomona.Models.Models;
using Pomona.Protos.Common;
using Pomona.Pwa.Client.Custom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Inventory
{
    public partial class WatchBase : CustomComponentBase
    {
        [Parameter] public int ItemTypeId { get; set; }
        [Parameter] public WatchModel Watch { get; set; } = new WatchModel();
        [Parameter] public EventCallback OnValidSubmit { get; set; }
        [Parameter] public bool EditMode { get; set; }
        [Parameter] public bool IsDisabled { get; set; } = false;
        [Parameter] public bool DisabledButton { get; set; } = true;
        [Parameter] public ElementReference ReferenceInput { get; set; }
        public IEnumerable<TypeProto> Brands { get; set; } = Enumerable.Empty<TypeProto>();
        public IEnumerable<TypeProto> Genders { get; set; } = Enumerable.Empty<TypeProto>();
        public IEnumerable<TypeProto> Providers { get; set; } = Enumerable.Empty<TypeProto>();


        protected override async Task OnInitializedAsync()
        {
            CultureInfo.NumberFormat.CurrencyPositivePattern = 2;
            Brands = (await Clients.Parametric().GetBrandsAsync(Empty)).Items;
            Genders = (await Clients.Parametric().GetGendersAsync(Empty)).Items;
            Providers = (await Clients.Parametric().GetProvidersAsync(Empty)).Items;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await ReferenceInput.FocusAsync();
        }

        protected async Task GetContract()
        {

        }


        protected override async Task OnParametersSetAsync()
        {
            //if (EditMode)
            //{
            //    try
            //    {
            //        await GetContract().ConfigureAwait(false);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}
        }


        public async Task OnPaymentBlur(FocusEventArgs e)
        {

        }

        public void ClearForm()
        {
            Watch = new WatchModel();
            IsDisabled = false;
            DisabledButton = true;
        }
    }
}
