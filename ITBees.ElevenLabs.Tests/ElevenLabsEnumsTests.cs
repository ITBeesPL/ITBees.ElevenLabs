using ITBees.ElevenLabs.Models;

namespace ITBees.ElevenLabs.Tests;

public class ElevenLabsEnumsTests
{
    [Theory]
    [InlineData(ElevenLabsModel.V4, "eleven_v4")]
    [InlineData(ElevenLabsModel.V4Turbo, "eleven_v4_turbo")]
    [InlineData(ElevenLabsModel.V3, "eleven_v3")]
    [InlineData(ElevenLabsModel.V3Conversational, "eleven_v3_conversational")]
    [InlineData(ElevenLabsModel.MultilingualV2, "eleven_multilingual_v2")]
    [InlineData(ElevenLabsModel.FlashV2_5, "eleven_flash_v2_5")]
    [InlineData(ElevenLabsModel.FlashV2, "eleven_flash_v2")]
    public void Model_MapsToTheApiIdentifier(ElevenLabsModel model, string apiString)
    {
        Assert.Equal(apiString, model.ToApiString());
    }

    [Fact]
    public void Model_EveryValueHasAnApiIdentifier()
    {
        foreach (var model in Enum.GetValues<ElevenLabsModel>())
        {
            Assert.StartsWith("eleven_", model.ToApiString());
        }
    }

    [Fact]
    public void Model_AnUnknownValueIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ElevenLabsModel)999).ToApiString());
    }

    /// <summary>The allowed values of output_format, as listed in the API reference.</summary>
    [Fact]
    public void OutputFormat_CoversExactlyTheDocumentedValues()
    {
        var documented = new[]
        {
            "alaw_8000", "mp3_22050_32", "mp3_24000_48", "mp3_44100_128", "mp3_44100_192", "mp3_44100_32",
            "mp3_44100_64", "mp3_44100_96", "opus_48000_128", "opus_48000_192", "opus_48000_32", "opus_48000_64",
            "opus_48000_96", "pcm_16000", "pcm_22050", "pcm_24000", "pcm_32000", "pcm_44100", "pcm_48000", "pcm_8000",
            "ulaw_8000", "wav_16000", "wav_22050", "wav_24000", "wav_32000", "wav_44100", "wav_48000", "wav_8000"
        };

        var actual = Enum.GetValues<ElevenLabsOutputFormat>().Select(x => x.ToApiString());

        Assert.Equal(documented.OrderBy(x => x), actual.OrderBy(x => x));
    }

    [Theory]
    [InlineData(ElevenLabsOutputFormat.Mp3_44100_128, "mp3")]
    [InlineData(ElevenLabsOutputFormat.Opus_48000_64, "opus")]
    [InlineData(ElevenLabsOutputFormat.Pcm_16000, "pcm")]
    [InlineData(ElevenLabsOutputFormat.Wav_44100, "wav")]
    [InlineData(ElevenLabsOutputFormat.Ulaw_8000, "ulaw")]
    [InlineData(ElevenLabsOutputFormat.Alaw_8000, "alaw")]
    public void OutputFormat_GivesTheFileExtension(ElevenLabsOutputFormat format, string extension)
    {
        Assert.Equal(extension, format.ToFileExtension());
    }
}
