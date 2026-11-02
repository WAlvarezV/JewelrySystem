using JewerlySystem.Common.Application.Voice;
using JewerlySystem.Common.Infrastructure.Voice;
using Microsoft.Extensions.DependencyInjection;

namespace JewerlySystem.Common.Infrastructure;

public static class VoiceInfrastructureExtensions
{
    public static IServiceCollection AddVoiceIntelligence(this IServiceCollection services)
    {
        services.AddHttpClient<IVoiceInterpreterService, GoogleGeminiVoiceService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        return services;
    }
}
