using Pomona.Protos.Inventory;
using Pomona.Pwa.Client.Custom;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Inventory
{
    public partial class DischargeBase : CustomComponentBase
    {
        public ItemProto Item { get; set; } = new ItemProto();
        public string DateOfEntry { get; set; }
        public string CostValue { get; set; }
        public string Provider { get; set; }
        public int SaleValue { get; set; }
        public bool IsWatch { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        private async Task GetItem()
        {
            var request = new ItemRequest
            {
                Reference = int.Parse(Pagination.Filter.Key),
                Description = Pagination.Filter.Other
            };
            Item = await Clients.Inventory().GetItemAsync(request);
            IsWatch = Item.ItemType.Id.Equals("5");
            DateOfEntry = Item.DateOfEntry.ToDateTime().ToString();
            CostValue = Item.CostValue.ToString("C0", CultureInfo);
            Provider = Item.Provider != null ? Item.Provider.FullName : "NO REGISTRA";
            StateHasChanged();
        }


        protected async Task FilteredSearch()
        {
            await GetItem();
        }

        protected void ClearFilter()
        {
            ClearPaginationFilter();
        }

        protected async Task DischargeItem()
        {
            if (SaleValue <= Item.CostValue)
            {
                await ErrorMessage($"El valor de venta no puede ser menor o igual al valor de costo");
                return;
            }
            if (await ConfirmMessage("Descargar Artículo", $"Descargar artículo del inventario. ¿Descargar {Item.ItemType.Name} refrencia: {Item.Reference}?", "question", "Si, Descargar."))
            {
                var request = new DischargeRequest { ItemId = Item.Id, SaleValue = SaleValue };
                await Clients.Inventory().DischargeItemAsync(request);
                await SuccessMessage($"Artículo Descargado.");
            }
        }
    }
}
