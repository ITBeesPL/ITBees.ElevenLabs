using ITBees.ElevenLabs.Interfaces;
using ITBees.ElevenLabs.Models;
using ITBees.ElevenLabs.Services;
using ITBees.ElevenLabs.Setup;
using ITBees.Interfaces.Platforms;
using Microsoft.Extensions.DependencyInjection;

namespace ITBees.ElevenLabs.Tests;

public class ElevenLabsSetupTests
{
    private static ServiceCollection ServicesWithPlatformSettings(FakePlatformSettingsService settings)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPlatformSettingsService>(settings);
        return services;
    }

    [Fact]
    public void AddElevenLabs_ReadsTheKeyFromThePlatformSettings()
    {
        var settings = new FakePlatformSettingsService { Settings = { ["ElevenLabsApiKey"] = "from-config" } };
        var services = ServicesWithPlatformSettings(settings);

        services.AddElevenLabs();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<ElevenLabsConnector>(provider.GetRequiredService<IElevenLabsConnector>());
        Assert.Equal("from-config", provider.GetRequiredService<IElevenLabsApiKeyProvider>().GetApiKey());
    }

    [Fact]
    public async Task AddElevenLabs_AMissingKeyFailsTheCall_NotTheStartOfTheApplication()
    {
        var services = ServicesWithPlatformSettings(new FakePlatformSettingsService());

        services.AddElevenLabs();
        using var provider = services.BuildServiceProvider();
        var connector = provider.GetRequiredService<IElevenLabsConnector>();

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.GetSubscriptionAsync());
        Assert.Equal("missing_api_key", exception.ErrorCode);
    }

    [Fact]
    public void AddElevenLabs_KeepsAKeyProviderRegisteredByTheHost()
    {
        var services = new ServiceCollection();
        var hostProvider = new FakeApiKeyProvider("from-database");
        services.AddSingleton<IElevenLabsApiKeyProvider>(hostProvider);

        services.AddElevenLabs();
        using var provider = services.BuildServiceProvider();

        Assert.Same(hostProvider, provider.GetRequiredService<IElevenLabsApiKeyProvider>());
        Assert.IsType<ElevenLabsConnector>(provider.GetRequiredService<IElevenLabsConnector>());
    }

    [Fact]
    public void AddElevenLabs_DisablesAutomaticRedirects()
    {
        var services = ServicesWithPlatformSettings(new FakePlatformSettingsService());
        services.AddElevenLabs();
        using var provider = services.BuildServiceProvider();

        HttpMessageHandler handler = provider.GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler(ElevenLabsConnector.HttpClientName);
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
        }

        var primary = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.False(primary.AllowAutoRedirect);
    }
}
