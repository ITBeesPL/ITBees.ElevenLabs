namespace ITBees.ElevenLabs.Models;

/// <summary>Filters of GET /v2/voices. Null values are left out of the request.</summary>
public class ElevenLabsVoicesQuery
{
    /// <summary>Searches in name, description, labels and category.</summary>
    public string? Search { get; set; }

    /// <summary>personal, community, default, workspace, non-default, non-community or saved.</summary>
    public string? VoiceType { get; set; }

    /// <summary>premade, cloned, generated or professional.</summary>
    public string? Category { get; set; }

    /// <summary>Voices with any of these "language" labels, e.g. pl.</summary>
    public List<string>? Languages { get; set; }

    /// <summary>At most 100; the API defaults to 10.</summary>
    public int? PageSize { get; set; }

    /// <summary><see cref="ElevenLabsVoicesPage.NextPageToken"/> of the previous page.</summary>
    public string? NextPageToken { get; set; }
}

public class ElevenLabsVoicesPage
{
    public List<ElevenLabsVoice> Voices { get; set; } = new();
    public bool HasMore { get; set; }
    public string? NextPageToken { get; set; }
}

/// <summary>The part of the voice description useful for choosing a voice.</summary>
public class ElevenLabsVoice
{
    public string VoiceId { get; set; } = "";
    public string? Name { get; set; }

    /// <summary>premade, cloned, generated or professional.</summary>
    public string? Category { get; set; }

    public string? Description { get; set; }

    /// <summary>Address of a sample of the voice.</summary>
    public string? PreviewUrl { get; set; }

    /// <summary>E.g. accent, gender, age, language.</summary>
    public Dictionary<string, string?>? Labels { get; set; }

    /// <summary>Languages ElevenLabs verified for this voice, each with its own sample.</summary>
    public List<ElevenLabsVoiceLanguage>? VerifiedLanguages { get; set; }

    public bool? IsLegacy { get; set; }

    /// <summary>Filled for voices shared through the Voice Library.</summary>
    public ElevenLabsVoiceSharing? Sharing { get; set; }
}

public class ElevenLabsVoiceLanguage
{
    public string? Language { get; set; }
    public string? ModelId { get; set; }
    public string? Accent { get; set; }
    public string? Locale { get; set; }
    public string? PreviewUrl { get; set; }
}

public class ElevenLabsVoiceSharing
{
    /// <summary>"notice_period" of a shared voice, in days.</summary>
    public int? NoticePeriod { get; set; }

    /// <summary>"disable_at_unix" of a shared voice (Unix time, seconds).</summary>
    public long? DisableAtUnix { get; set; }
}
