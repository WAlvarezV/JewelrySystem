using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Pomona.Models.Models;
using Pomona.Protos;
using Pomona.Pwa.Client.Custom;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Contract
{
    public partial class ContractBase : CustomComponentBase
    {
        [Parameter] public IdentificationProto Id { get; set; } = new IdentificationProto();
        [Parameter] public ContractModel Contract { get; set; }
        [Parameter] public EventCallback OnValidSubmit { get; set; }
        [Parameter] public bool EditMode { get; set; }
        [Parameter] public bool IsDisabled { get; set; } = false;
        [Parameter] public bool DisabledButton { get; set; } = true;


        public async Task OnBlur(FocusEventArgs e)
        {
            //var response = await Clients.Tercero().GetByIdentificationAsync(Id, Header);
            //if (response.TerceroProto != null)
            //{
            //    User.Tercero.Id = response.TerceroProto.Id;
            //    User.Tercero.NombreCompleto = response.TerceroProto.NombreCompleto;
            //    User.Tercero.Celular = response.TerceroProto.Celular;
            //    User.Tercero.Direccion = response.TerceroProto.Direccion;
            //    User.Email = response.TerceroProto.Email;
            //    IsDisabled = true;
            //    DisabledButton = false;
            //}
            //else
            //{
            //    if (await ConfirmMessage("¡El tercero no existe!", "¿Desea crearlo ahora?", "question", "Crear Tercero"))
            //        NavigationManager.NavigateTo(CreateTercero.ComponentPath);
            //    else
            //        ClearForm();
            //}
        }

        public void ClearForm()
        {
            Contract = new ContractModel();
            Id = new IdentificationProto();
            IsDisabled = false;
            DisabledButton = true;
        }
    }
}
