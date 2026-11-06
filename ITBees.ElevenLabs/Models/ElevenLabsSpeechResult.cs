namespace ITBees.ElevenLabs.Models;

public class ElevenLabsSpeechResult
{
    /// <summary>The whole audio file.</summary>
    public byte[] Audio { get; set; } = Array.Empty<byte>();

    /// <summary>Content type reported by the API, e.g. audio/mpeg.</summary>
    public string? ContentType { get; set; }

    public ElevenLabsOutputFormat OutputFormat { get; set; }

    /// <summary>Extension for saving the file, without the dot, e.g. mp3.</summary>
    public string FileExtension => OutputFormat.ToFileExtension();

    /// <summary>"request-id" response header, useful when reporting a problem to ElevenLabs.</summary>
    public string? RequestId { get; set; }

    /// <summary>"character-cost" response header: the credits billed for this generation.</summary>
    public int? CharacterCost { get; set; }
}
