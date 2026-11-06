namespace ITBees.ElevenLabs.Models;

/// <summary>The API still calls credits "characters" here.</summary>
public class ElevenLabsSubscription
{
    /// <summary>Plan, e.g. free, starter.</summary>
    public string? Tier { get; set; }

    /// <summary>E.g. active.</summary>
    public string? Status { get; set; }

    /// <summary>Credits used in the current billing period.</summary>
    public long CharacterCount { get; set; }

    /// <summary>Credits available in the current billing period.</summary>
    public long CharacterLimit { get; set; }

    public long RemainingCharacters => Math.Max(0, CharacterLimit - CharacterCount);

    /// <summary>When the credits reset (Unix time, seconds).</summary>
    public long? NextCharacterCountResetUnix { get; set; }
}
