using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Extensions
{
    public static class IJSExtensions
    {
        public static async ValueTask InitializeInactivityTimer<T>(this IJSRuntime js,
            DotNetObjectReference<T> dotNetObjectReference) where T : class
        {
            await js.InvokeVoidAsync("initializeInactivityTimer", dotNetObjectReference);
        }


        public static ValueTask<object> SaveFileAs(this IJSRuntime js, string fileName, byte[] file)
        => js.InvokeAsync<object>("saveAsFile", fileName, Convert.ToBase64String(file));


        public static ValueTask ShowMessage(this IJSRuntime js, string title, string message, string type)
        {
            return js.InvokeVoidAsync("Swal.fire", title, message, type);
        }

        public static ValueTask WaitMessage(this IJSRuntime js, string message)
        {
            return js.InvokeVoidAsync("messageFunctions.WaitMessage", message);
        }

        public static ValueTask<bool> ConfirmMessage(this IJSRuntime js, string title, string message, string type, string confirmText)
        {
            return js.InvokeAsync<bool>("CustomConfirm", title, message, type, confirmText);
        }

        public static ValueTask CloseMessage(this IJSRuntime js)
        {
            return js.InvokeVoidAsync("Swal.close");
        }
    }
}
