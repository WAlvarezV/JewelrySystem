using Microsoft.AspNetCore.Components;
using Pomona.Models.Models;
using Pomona.Protos.Inventory;
using Pomona.Pwa.Client.Custom;
using System;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Inventory
{
    public partial class CreateWatchBase : CustomComponentBase
    {
        public WatchModel Watch { get; set; } = new WatchModel();
        public ElementReference ReferenceInput { get; set; }

        protected async Task Create()
        {
            try
            {
                await WaitMessage("Registrando Nuevo Reloj.");
                var objToInsert = Mapper.Map<ItemProto>(Watch);
                var res = await Clients.Inventory().RegisterItemAsync(objToInsert);
                //Watch = new WatchModel();
                ClearSoftModel();
                await SuccessMessage($"¡Reloj Registrado con Id:{res.Message} !");
            }
            catch (Exception ex)
            {
                await CloseMessage();
                await ErrorMessage($"Watch Create Exception => Message {ex.Message}");
            }
        }

        private void ClearSoftModel()
        {
            Watch.Reference = 0;
            Watch.SaleValue = 0;
            Watch.CostValue = 0;
            Watch.CaseNumber = string.Empty;
            Watch.Description = string.Empty;
        }
    }
}