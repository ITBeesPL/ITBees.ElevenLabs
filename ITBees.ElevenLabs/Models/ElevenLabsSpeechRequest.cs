namespace ITBees.ElevenLabs.Models;

public class ElevenLabsSpeechRequest
{
    public string Text { get; set; } = "";

    /// <summary>
    /// Voice identifier. One voice can speak every language of the model, so the same value gives the same voice
    /// in all language versions of a message.
    /// </summary>
    public string VoiceId { get; set; } = "";

    public ElevenLabsModel Model { get; set; } = ElevenLabsModel.V4;

    /// <summary>
    /// ISO 639-1 code (pl, en, de, uk) enforcing the language, which helps with short phrases. Leave null to let
    /// the model detect it. Not sent for <see cref="ElevenLabsModel.MultilingualV2"/>, which does not support it.
    /// </summary>
    public string? LanguageCode { get; set; }

    public ElevenLabsOutputFormat OutputFormat { get; set; } = ElevenLabsOutputFormat.Mp3_44100_128;

    /// <summary>Overrides the settings stored with the voice for this request only; null uses the stored ones.</summary>
    public ElevenLabsVoiceSettings? VoiceSettings { get; set; }

    /// <summary>Repeating a request with the same seed gives the same audio on a best-effort basis, not guaranteed.</summary>
    public uint? Seed { get; set; }
}

/// <summary>Null values are left out of the request.</summary>
public class ElevenLabsVoiceSettings
{
    public double? Stability { get; set; }
    public double? SimilarityBoost { get; set; }
    public double? Style { get; set; }
    public double? Speed { get; set; }
    public bool? UseSpeakerBoost { get; set; }
}
