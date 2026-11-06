namespace ITBees.ElevenLabs.Models;

/// <summary>Text to speech models. <see cref="ElevenLabsModelExtensions.ToApiString"/> gives the API identifier.</summary>
public enum ElevenLabsModel
{
    /// <summary>"eleven_v4" - 90+ languages, recommended for content generated ahead of time.</summary>
    V4,

    /// <summary>"eleven_v4_turbo" - 90+ languages, low latency, half the price.</summary>
    V4Turbo,

    /// <summary>"eleven_v3" - 70+ languages.</summary>
    V3,

    /// <summary>"eleven_v3_conversational" - 70+ languages, low latency.</summary>
    V3Conversational,

    /// <summary>"eleven_multilingual_v2" - 29 languages, detects the language from the text.</summary>
    MultilingualV2,

    /// <summary>"eleven_flash_v2_5" - 32 languages, low latency.</summary>
    FlashV2_5,

    /// <summary>"eleven_flash_v2" - English only, low latency.</summary>
    FlashV2
}
