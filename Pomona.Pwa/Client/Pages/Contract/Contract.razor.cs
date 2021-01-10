using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Pomona.Models.Models;
using Pomona.Protos;
using Pomona.Pwa.Client.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Contract
{
    public partial class ContractBase : CustomComponentBase
    {
        [Parameter] public int ContractId { get; set; }
        [Parameter] public ContractModel Contract { get; set; } = new ContractModel();
        [Parameter] public EventCallback OnValidSubmit { get; set; }
        [Parameter] public bool EditMode { get; set; }
        [Parameter] public bool IsDisabled { get; set; } = false;
        [Parameter] public bool DisabledButton { get; set; } = true;
        public IEnumerable<TypeProto> IdentificationTypes { get; set; }
        [Parameter] public IEnumerable<PaymentProto> Payments { get; set; } = Enumerable.Empty<PaymentProto>();

        public string ContractValue { get; set; } = "$0";
        public string PaymentsValue { get; set; } = "$0";
        public string BalanceValue { get; set; } = "$0";
        public int NewPaymenValue { get; set; }

        public ContractResponse Response { get; set; } = new ContractResponse();

        protected override void OnInitialized()
        {
            CultureInfo.NumberFormat.CurrencyPositivePattern = 2;
            IdentificationTypes = new TypeProto[] { new TypeProto { Id = "1", Name = "CEDULA DE CIUDADANIA" }, new TypeProto { Id = "2", Name = "NIT" } };

        }

        protected async Task GetContract()
        {
            try
            {
                Response = await Clients.Contract().GetContractByIdAsync(new IdProto { Id = ContractId });
                Contract = Mapper.Map<ContractModel>(Response.Contract);
                Payments = Response.Payments;
                ContractValue = ((int)Contract.Value).ToString("C0", CultureInfo);
                PaymentsValue = (Payments.Sum(x => x.Value)).ToString("C0", CultureInfo);
                BalanceValue = ((int)Contract.Balance).ToString("C0", CultureInfo);
            }
            catch (Exception ex)
            {
                var error = $"GetContract ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                Console.WriteLine(error);
            }
        }


        protected override async Task OnParametersSetAsync()
        {
            if (EditMode)
            {
                try
                {
                    await GetContract().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        protected async Task RegisterNewPayment(int value)
        {
            try
            {
                await WaitMessage("Registrando Abono.");
                var payment = new PaymentProto { EntityId = ContractId, Value = value };
                var res = await Clients.Contract().RegisterPaymentAsync(payment);
                await GetContract().ConfigureAwait(false);
                await SuccessMessage(res.Response);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                await CloseMessage();
                await ErrorMessage($"Contract Create Exception => Message {ex.Message}");
            }
        }

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

        public async Task OnPaymentBlur(FocusEventArgs e)
        {
            await Task.FromResult(Contract.Balance = Contract.Value - Contract.Payment);
        }

        public void ClearForm()
        {
            Contract = new ContractModel();
            IsDisabled = false;
            DisabledButton = true;
        }
    }
}
