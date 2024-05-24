using Microsoft.AspNetCore.Components;
using Pomona.Protos.Inventory;
using Pomona.Pwa.Client.Custom;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Inventory
{
    public class ItemBase : CustomComponentBase
    {
        [Parameter] public ItemProto Item { get; set; } = new ItemProto();
        [Parameter] public int Reference { get; set; }
        [Parameter] public int ItemTypeId { get; set; }
        public string DateOfEntry { get; set; }
        public string DateOfSale { get; set; }
        public string CostValue { get; set; }
        public string SaleValue { get; set; }
        public string Provider { get; set; }
        public string State { get; set; }
        public bool IsWatch { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (Item != null && Item.Id > 0)
                SetItemData();

            if (Reference > 0)
                await GetItem();
        }

        private async Task GetItem()
        {
            Item = await Clients.Inventory().GetItemAsync(new ItemRequest { Reference = Reference, ItemTypeId = ItemTypeId });
            SetItemData();
        }

        private void SetItemData()
        {
            IsWatch = Item.ItemType.Id.Equals("5");
            DateOfEntry = Item.DateOfEntry.ToDateTime().ToString();
            CostValue = Item.CostValue.ToString("C0", CultureInfo);
            Provider = Item.Provider != null ? Item.Provider.FullName : "NO REGISTRA";
            SaleValue = Item.SaleValue.ToString("C0", CultureInfo);
            if (Item.Active)
            {
                State = "En Stock";
                DateOfSale = string.Empty;
            }
            else
            {
                State = "Vendido";
                DateOfSale = Item.DateOfSale.ToDateTime().ToString();
            }
        }
    }
}
