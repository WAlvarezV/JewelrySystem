using Microsoft.AspNetCore.Components;
using Pomona.Models.Models;
using Pomona.Protos;
using Pomona.Pwa.Client.Custom;
using System;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Contract
{
    public partial class EditContractBase : CustomComponentBase
    {
        [Parameter] public int Id { get; set; }
        public ContractModel Contract { get; set; } = new ContractModel();

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected async Task Save()
        {
            try
            {
                await WaitMessage("Registrando Contrato.");
                var objToInsert = Mapper.Map<ContractProto>(Contract);
                var res = await Clients.Contract().RegisterContractAsync(objToInsert);
                Contract = new ContractModel();
                await SuccessMessage("¡Contrato Registrado!");
            }
            catch (Exception ex)
            {
                await CloseMessage();
                await ErrorMessage($"Contract Create Exception => Message {ex.Message}");
            }
        }


    }
}
