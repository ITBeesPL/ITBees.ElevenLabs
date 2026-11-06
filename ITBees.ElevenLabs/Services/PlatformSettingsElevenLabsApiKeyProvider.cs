using ITBees.ElevenLabs.Interfaces;
using ITBees.Interfaces.Platforms;

namespace ITBees.ElevenLabs.Services;

/// <summary>Reads the API key from the platform settings (config.json), like ChatGptApiKey in ITBees.ChatGpt.</summary>
public class PlatformSettingsElevenLabsApiKeyProvider : IElevenLabsApiKeyProvider
{
    public const string SettingName = "ElevenLabsApiKey";

    private readonly IPlatformSettingsService _platformSettingsService;

    public PlatformSettingsElevenLabsApiKeyProvider(IPlatformSettingsService platformSettingsService)
    {
        _platformSettingsService = platformSettingsService;
    }

    public string? GetApiKey()
    {
        return _platformSettingsService.GetSetting(SettingName);
    }
}
