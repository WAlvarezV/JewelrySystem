
using AutoMapper;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Pomona.Pwa.Client.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddHttpClient("Pomona.Pwa.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
                .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            // Supply HttpClient instances that include access tokens when making requests to the server project
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Pomona.Pwa.ServerAPI"));

            builder.Services.AddApiAuthorization();

            builder.Services.AddScoped(services =>
              {
                  var baseAddressMessageHandler = services.GetRequiredService<BaseAddressAuthorizationMessageHandler>();
                  baseAddressMessageHandler.InnerHandler = new HttpClientHandler();
                  var grpcWebHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, baseAddressMessageHandler);
                  var channel = GrpcChannel.ForAddress(builder.HostEnvironment.BaseAddress, new GrpcChannelOptions { HttpHandler = grpcWebHandler });

                  return channel;
              });

            builder.Services.AddScoped<IServiceClient, ServiceClient>(services =>
            {
                var channel = services.GetService<GrpcChannel>();
                return new ServiceClient(channel);
            });


            ConfigureServices(builder.Services);

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {


            //services.AddFileReaderService(options => options.InitializeOnFirstCall = true);
            //services.AddSingleton<IFileImportService, FileImportService>();
            services.AddAutoMapper(typeof(Program));
        }
    }
}
