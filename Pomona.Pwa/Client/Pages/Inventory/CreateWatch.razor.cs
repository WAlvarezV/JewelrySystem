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

        protected async Task Create()
        {
            try
            {
                await WaitMessage("Registrando Nuevo Reloj.");
                var objToInsert = Mapper.Map<ItemProto>(Watch);
                var res = await Clients.Inventory().RegisterItemAsync(objToInsert);
                Watch = new WatchModel();
                await SuccessMessage("¡Reloj Registrado!");
            }
            catch (Exception ex)
            {
                await CloseMessage();
                await ErrorMessage($"Watch Create Exception => Message {ex.Message}");
            }
        }
    }
}