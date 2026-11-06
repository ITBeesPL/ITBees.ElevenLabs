using ITBees.ElevenLabs.Models;

namespace ITBees.ElevenLabs.Interfaces;

/// <summary>
/// Connector to the ElevenLabs API. Every failure is thrown as <see cref="ElevenLabsApiException"/>; a cancelled
/// call throws <see cref="OperationCanceledException"/>.
/// </summary>
public interface IElevenLabsConnector
{
    /// <summary>
    /// POST /v1/text-to-speech/{voice_id} - generates the speech and returns the whole audio file in memory, ready
    /// to be saved. Uses credits of the account.
    /// </summary>
    Task<ElevenLabsSpeechResult> TextToSpeechAsync(ElevenLabsSpeechRequest request, CancellationToken ct = default);

    /// <summary>
    /// GET /v2/voices - one page of the voices of the account ("My Voices" and the default voices). Voices from the
    /// Voice Library appear here after they are added to "My Voices".
    /// </summary>
    Task<ElevenLabsVoicesPage> GetVoicesAsync(ElevenLabsVoicesQuery? query = null, CancellationToken ct = default);

    /// <summary>
    /// GET /v1/user/subscription - the plan and the credits used. Generates nothing, so it also serves as a check
    /// that the API key works.
    /// </summary>
    Task<ElevenLabsSubscription> GetSubscriptionAsync(CancellationToken ct = default);
}
