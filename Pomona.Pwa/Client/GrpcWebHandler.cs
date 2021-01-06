using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System;
using System.Net.Http;

namespace Pomona.Pwa.Client
{
    internal class GrpcWebHandler
    {
        private object grpcWeb;
        private BaseAddressAuthorizationMessageHandler baseAddressMessageHandler;

        public GrpcWebHandler(object grpcWeb, BaseAddressAuthorizationMessageHandler baseAddressMessageHandler)
        {
            this.grpcWeb = grpcWeb;
            this.baseAddressMessageHandler = baseAddressMessageHandler;
        }

        public static implicit operator HttpMessageHandler(GrpcWebHandler v)
        {
            throw new NotImplementedException();
        }
    }
}