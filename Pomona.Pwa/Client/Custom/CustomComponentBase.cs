using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using Pomona.Protos.Common;
using Pomona.Pwa.Client.Extensions;
using Pomona.Pwa.Client.Services;
using System.Globalization;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Custom
{
    public abstract class CustomComponentBase : ComponentBase
    {
        [CascadingParameter] Task<AuthenticationState> AuthenticationStateTask { get; set; }
        [Inject] protected IServiceClient Clients { get; set; }
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected IMapper Mapper { get; set; }
        [Inject] IAccessTokenProvider TokenProvider { get; set; }
        protected Empty Empty { get; set; } = new Empty();
        protected Metadata Header { get; set; } = new Metadata();
        protected CultureInfo CultureInfo { get; set; } = new CultureInfo("es-US", false);
        protected NumberFormatInfo NumberFormatInfo { get; set; } = new CultureInfo("es-US", false).NumberFormat;
        protected Pagination Pagination { get; set; } = new Pagination
        {
            Page = 1,
            Records = 10,
            Filter = new Filter { State = "0", Type = "0" }
        };
        protected int Pages;
        protected string UserId { get; set; } = string.Empty;
        protected string UserRole { get; set; } = string.Empty;
        //protected string AuthorizedRoles { get; set; } = Constant.AdministratorRole;

        protected override void OnInitialized()
        {
            NumberFormatInfo.CurrencyPositivePattern = 2;
            CultureInfo.NumberFormat = NumberFormatInfo;
            base.OnInitialized();
        }

        protected async Task SuccessMessage(string msg) => await JSRuntime.ShowMessage("¡Muy bien!", msg, "success");

        protected async Task SaveFile(string fileName, byte[] file) => await JSRuntime.SaveFileAs(fileName, file);

        protected async Task ErrorMessage(string msg) => await JSRuntime.ShowMessage("¡Error!", msg, "error");

        protected async Task InfoMessage(string msg) => await JSRuntime.ShowMessage("¡Información!", msg, "info");

        protected async Task WaitMessage(string msg) => await JSRuntime.WaitMessage(msg);

        protected async Task<bool> ConfirmMessage(string title, string message, string type, string confirmText)
        => await JSRuntime.ConfirmMessage(title, message, type, confirmText);

        protected async Task CloseMessage() => await JSRuntime.CloseMessage();

        /*
        protected async Task SetToken()
        {
            await SetUserClaims();
            var tokenResult = await TokenProvider.RequestAccessToken();
            if (tokenResult.TryGetToken(out var accessToken))
            {
                Header.Add("Authorization", $"Bearer {accessToken.Value}");
            }
            else
            {
                Console.WriteLine($"Bad TryGetToken");
            }
        }

        protected async Task SetUserClaims()
        {
            try
            {
                var authState = await AuthenticationStateTask;
                var user = authState.User;
                if (user.Identity.IsAuthenticated)
                {
                    UserId = user.FindFirst(c => c.Type == "sub")?.Value;
                    UserRole = user.FindFirst(c => c.Type == "role")?.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetUserClaims Exception => Message {ex.Message} - InnerExceptionMessage {(ex.InnerException != null ? ex.InnerException.Message : "No InnerException")}");
            }
        }
        */

        //public async Task DownloadFile(string path)
        //{
        //    var fileName = Path.GetFileName(path);
        //    var request = new FileRequest { Path = path };
        //    if ((await Clients.File().FileExistsAsync(request)).Exists)
        //    {
        //        using var call = Clients.File().DownloadStream(request);
        //        try
        //        {
        //            var result = new StringBuilder();
        //            await WaitMessage($"Descargando: {fileName}");
        //            await foreach (var message in call.ResponseStream.ReadAllAsync())
        //            {
        //                result.Append(Encoding.UTF8.GetString(message.Content.ToByteArray()));
        //            }
        //            await CloseMessage();
        //            await SaveFile(fileName, Convert.FromBase64String(result.ToString()));
        //        }
        //        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        //        {
        //            Console.WriteLine("Stream cancelled.");
        //        }
        //    }
        //    else
        //    {
        //        await InfoMessage($"El archivo \"{fileName}\" no se encuentra en el servidor.");
        //    }
        //}

        protected void ClearPaginationFilter()
        {
            Pagination.Page = 1;
            Pagination.Filter = new Filter { Key = string.Empty, State = "0", Type = "0", Other = string.Empty };
        }
    }
}

