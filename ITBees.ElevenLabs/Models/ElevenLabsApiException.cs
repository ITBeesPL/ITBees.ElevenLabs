namespace ITBees.ElevenLabs.Models;

/// <summary>
/// Error answered by the ElevenLabs API, no connection to it, or a call that could not be sent. The message is in
/// Polish and can be shown to the operator as it is.
/// </summary>
public class ElevenLabsApiException : Exception
{
    public ElevenLabsApiException(string errorCode, string message, int statusCode, string? requestId = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        RequestId = requestId;
    }

    /// <summary>
    /// The code answered by the API (e.g. unauthorized for a wrong key, insufficient_credits, voice_not_found,
    /// concurrent_limit_exceeded). When it gives none: unauthorized, payment_required, forbidden, not_found,
    /// bad_request, too_many_requests, server_error or http_&lt;status&gt;. Without a response: missing_api_key,
    /// missing_voice_id, empty_text, connection_failed or timeout. After a successful status: empty_response or
    /// unexpected_response.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>HTTP status of the response; 0 when there was no response.</summary>
    public int StatusCode { get; }

    /// <summary>Identifier of the request at ElevenLabs, for reporting the problem to them.</summary>
    public string? RequestId { get; }
}
