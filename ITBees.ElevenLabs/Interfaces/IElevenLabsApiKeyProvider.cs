namespace ITBees.ElevenLabs.Interfaces;

/// <summary>
/// Source of the API key, read before every call so a changed key works without a restart. The default reads
/// the platform settings; a host keeping the key elsewhere (e.g. in a database row edited in the admin panel)
/// registers its own implementation.
/// </summary>
public interface IElevenLabsApiKeyProvider
{
    /// <summary>The API key, or null / empty when it is not configured.</summary>
    string? GetApiKey();
}
