using ITBees.ElevenLabs.Interfaces;
using ITBees.ElevenLabs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ITBees.ElevenLabs.Setup;

public static class ElevenLabsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IElevenLabsConnector"/>. The key comes from the "ElevenLabsApiKey" platform setting, so the
    /// host provides IPlatformSettingsService - or registers its own <see cref="IElevenLabsApiKeyProvider"/> instead.
    /// </summary>
    public static IServiceCollection AddElevenLabs(this IServiceCollection services)
    {
        services.TryAddTransient<IElevenLabsApiKeyProvider, PlatformSettingsElevenLabsApiKeyProvider>();

        // 2 min: generating a long text can take a while.
        services.AddHttpClient<IElevenLabsConnector, ElevenLabsConnector>(ElevenLabsConnector.HttpClientName,
                client => client.Timeout = TimeSpan.FromMinutes(2))
            // No automatic redirects: HttpClient drops only the Authorization header on a redirect, so the custom
            // "xi-api-key" header would follow a 3xx to another host.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        return services;
    }
}
