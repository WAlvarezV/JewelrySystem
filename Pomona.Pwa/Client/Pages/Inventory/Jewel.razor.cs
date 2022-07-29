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
    public partial class JewelBase : CustomComponentBase
    {
        [Parameter] public int ItemTypeId { get; set; }
        [Parameter] public JewelModel Jewel { get; set; } = new JewelModel();
        [Parameter] public EventCallback OnValidSubmit { get; set; }
        [Parameter] public bool EditMode { get; set; }
        [Parameter] public bool IsDisabled { get; set; } = false;
        [Parameter] public bool DisabledButton { get; set; } = true;
        [Parameter] public ElementReference FirstInput { get; set; }
        public IEnumerable<TypeProto> Providers { get; set; } = Enumerable.Empty<TypeProto>();

        public string Tittle { get; set; }
        public int GramSaleValue { get; set; }
        public int ProbableProfit { get; set; }
        public bool NeedLength { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Jewel.ItemTypeId = ItemTypeId;
            ConfigForm();
            CultureInfo.NumberFormat.CurrencyPositivePattern = 2;
            Providers = (await Clients.Parametric().GetProvidersAsync(Empty)).Items;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await FirstInput.FocusAsync();
        }

        public void OnBlur(FocusEventArgs args)
        {
            Jewel.CostValue = (int)(Jewel.GramValue * Jewel.Weight);
            Jewel.SaleValue = (int)(GramSaleValue * Jewel.Weight);

            ProbableProfit = Jewel.SaleValue - Jewel.CostValue;
        }

        public void ClearForm()
        {
            Jewel = new JewelModel();
            IsDisabled = false;
            DisabledButton = true;
        }

        private void ConfigForm()
        {
            NeedLength = ItemTypeId.Equals(3) || ItemTypeId.Equals(4);
            Tittle = ItemTypeId switch
            {
                1 => "Anillo",
                2 => "Aretes",
                3 => "Cadena",
                4 => "Pulsera",
                6 => "Dije",
                _ => "Joya",
            };
        }
    }
}
