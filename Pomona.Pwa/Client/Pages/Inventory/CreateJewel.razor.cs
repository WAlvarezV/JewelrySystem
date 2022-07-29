using Microsoft.AspNetCore.Components;
using Pomona.Models.Models;
using Pomona.Protos.Inventory;
using Pomona.Pwa.Client.Custom;
using System;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Inventory
{
    public partial class CreateJewelBase : CustomComponentBase
    {
        public JewelModel Jewel { get; set; } = new JewelModel();
        [Parameter] public int ItemTypeId { get; set; }
        public ElementReference ReferenceInput { get; set; }

        protected async Task Create()
        {
            try
            {
                await WaitMessage("Registrando Nueva joya.");
                var objToInsert = Mapper.Map<ItemProto>(Jewel);
                var res = await Clients.Inventory().RegisterItemAsync(objToInsert);
                ClearSoftModel();
                await SuccessMessage($"¡Joya Registrada con Id:{res.Message} !");
            }
            catch (Exception ex)
            {
                await CloseMessage();
                await ErrorMessage($"Jewel Create Exception => Message {ex.Message}");
            }
        }

        private void ClearSoftModel()
        {
            Jewel.Reference = 0;
            Jewel.SaleValue = 0;
            Jewel.CostValue = 0;
            Jewel.Weight = 0;
            Jewel.Description = string.Empty;
        }
    }
}