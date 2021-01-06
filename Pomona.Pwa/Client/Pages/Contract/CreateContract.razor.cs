using Pomona.Models.Models;
using Pomona.Pwa.Client.Custom;
using System;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Contract
{
    public partial class CreateContractBase : CustomComponentBase
    {
        public ContractModel Contract { get; set; } = new ContractModel();

        protected async Task Create()
        {
            try
            {
                await WaitMessage("Registrando Contrato.");
                //var objToInsert = Mapper.Map<TerceroProto>(TerceroModel);
                //var res = await Clients.Tercero().CreateAsync(new TerceroRequest { TerceroProto = objToInsert }, Header);
                Contract = new ContractModel();
                await SuccessMessage("Tercero creado");
            }
            catch (Exception ex)
            {
                await CloseMessage();
                await ErrorMessage($"Contract Create Exception => Message {ex.Message}");
            }
        }
    }
}
