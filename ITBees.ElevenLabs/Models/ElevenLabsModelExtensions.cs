namespace ITBees.ElevenLabs.Models;

public static class ElevenLabsModelExtensions
{
    public static string ToApiString(this ElevenLabsModel model)
    {
        return model switch
        {
            ElevenLabsModel.V4 => "eleven_v4",
            ElevenLabsModel.V4Turbo => "eleven_v4_turbo",
            ElevenLabsModel.V3 => "eleven_v3",
            ElevenLabsModel.V3Conversational => "eleven_v3_conversational",
            ElevenLabsModel.MultilingualV2 => "eleven_multilingual_v2",
            ElevenLabsModel.FlashV2_5 => "eleven_flash_v2_5",
            ElevenLabsModel.FlashV2 => "eleven_flash_v2",
            _ => throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown ElevenLabs model.")
        };
    }

    /// <summary>The API documents language_code as not supported for the multilingual v2 models.</summary>
    internal static bool SupportsLanguageCode(this ElevenLabsModel model)
    {
        return model != ElevenLabsModel.MultilingualV2;
    }
}
