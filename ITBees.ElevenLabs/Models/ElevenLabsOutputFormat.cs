namespace ITBees.ElevenLabs.Models;

/// <summary>
/// Audio format of the generated speech: codec_sampleRate_bitrate. The names are the API values, only capitalized.
/// Mp3_44100_192 requires the Creator plan or above; Pcm_44100 and Wav_44100 the Pro plan or above.
/// </summary>
public enum ElevenLabsOutputFormat
{
    Mp3_22050_32,
    Mp3_24000_48,
    Mp3_44100_32,
    Mp3_44100_64,
    Mp3_44100_96,
    Mp3_44100_128,
    Mp3_44100_192,
    Opus_48000_32,
    Opus_48000_64,
    Opus_48000_96,
    Opus_48000_128,
    Opus_48000_192,
    Pcm_8000,
    Pcm_16000,
    Pcm_22050,
    Pcm_24000,
    Pcm_32000,
    Pcm_44100,
    Pcm_48000,
    Wav_8000,
    Wav_16000,
    Wav_22050,
    Wav_24000,
    Wav_32000,
    Wav_44100,
    Wav_48000,
    Ulaw_8000,
    Alaw_8000
}

public static class ElevenLabsOutputFormatExtensions
{
    public static string ToApiString(this ElevenLabsOutputFormat format)
    {
        return format.ToString().ToLowerInvariant();
    }

    /// <summary>File extension without the dot: mp3, opus, pcm, wav, ulaw or alaw.</summary>
    public static string ToFileExtension(this ElevenLabsOutputFormat format)
    {
        return format.ToApiString().Split('_')[0];
    }
}
