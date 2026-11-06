using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITBees.ElevenLabs.Interfaces;
using ITBees.ElevenLabs.Models;

namespace ITBees.ElevenLabs.Services;

public class ElevenLabsConnector : IElevenLabsConnector
{
    public const string HttpClientName = "ElevenLabsApi";
    public const string ApiUrl = "https://api.elevenlabs.io";

    private const string ApiKeyHeader = "xi-api-key";

    /// <summary>The API's own error text is appended to our message up to this length.</summary>
    private const int MaxApiMessageLength = 300;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly IElevenLabsApiKeyProvider _apiKeyProvider;

    public ElevenLabsConnector(HttpClient httpClient, IElevenLabsApiKeyProvider apiKeyProvider)
    {
        _httpClient = httpClient;
        _apiKeyProvider = apiKeyProvider;
    }

    public async Task<ElevenLabsSpeechResult> TextToSpeechAsync(ElevenLabsSpeechRequest speechRequest,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(speechRequest.VoiceId))
        {
            throw new ElevenLabsApiException("missing_voice_id", "Nie wskazano głosu ElevenLabs.", 0);
        }

        if (string.IsNullOrWhiteSpace(speechRequest.Text))
        {
            throw new ElevenLabsApiException("empty_text", "Tekst komunikatu jest pusty.", 0);
        }

        var voiceId = Uri.EscapeDataString(speechRequest.VoiceId.Trim());
        var outputFormat = speechRequest.OutputFormat.ToApiString();
        using var request = CreateRequest(HttpMethod.Post, $"/v1/text-to-speech/{voiceId}?output_format={outputFormat}");

        var body = new TextToSpeechBody(
            speechRequest.Text,
            speechRequest.Model.ToApiString(),
            speechRequest.Model.SupportsLanguageCode() ? NullIfBlank(speechRequest.LanguageCode) : null,
            speechRequest.VoiceSettings,
            speechRequest.Seed);
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await SendAsync(request, ct);
        var audio = await response.Content.ReadAsByteArrayAsync(ct);
        var requestId = FirstHeaderValue(response, "request-id");
        if (audio.Length == 0)
        {
            throw new ElevenLabsApiException("empty_response", "ElevenLabs zwrócił pusty plik audio.",
                (int)response.StatusCode, requestId);
        }

        return new ElevenLabsSpeechResult
        {
            Audio = audio,
            ContentType = response.Content.Headers.ContentType?.MediaType,
            OutputFormat = speechRequest.OutputFormat,
            RequestId = requestId,
            CharacterCost = int.TryParse(FirstHeaderValue(response, "character-cost"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var characterCost)
                ? characterCost
                : null
        };
    }

    public async Task<ElevenLabsVoicesPage> GetVoicesAsync(ElevenLabsVoicesQuery? query = null,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Get, BuildVoicesPath(query));
        return await SendForJsonAsync<ElevenLabsVoicesPage>(request, ct);
    }

    public async Task<ElevenLabsSubscription> GetSubscriptionAsync(CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "/v1/user/subscription");
        return await SendForJsonAsync<ElevenLabsSubscription>(request, ct);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string pathAndQuery)
    {
        var apiKey = _apiKeyProvider.GetApiKey()?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ElevenLabsApiException("missing_api_key", "Brak klucza API ElevenLabs w konfiguracji aplikacji.", 0);
        }

        var request = new HttpRequestMessage(method, ApiUrl + pathAndQuery);
        request.Headers.Add(ApiKeyHeader, apiKey);
        return request;
    }

    private static string BuildVoicesPath(ElevenLabsVoicesQuery? query)
    {
        var parameters = new List<string>();

        void AddIfSet(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parameters.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
            }
        }

        AddIfSet("search", query?.Search);
        AddIfSet("voice_type", query?.VoiceType);
        AddIfSet("category", query?.Category);
        foreach (var language in query?.Languages ?? new List<string>())
        {
            AddIfSet("language", language);
        }

        AddIfSet("page_size", query?.PageSize?.ToString(CultureInfo.InvariantCulture));
        AddIfSet("next_page_token", query?.NextPageToken);

        return parameters.Count == 0 ? "/v2/voices" : $"/v2/voices?{string.Join("&", parameters)}";
    }

    private async Task<T> SendForJsonAsync<T>(HttpRequestMessage request, CancellationToken ct) where T : class
    {
        using var response = await SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var statusCode = (int)response.StatusCode;
        var requestId = FirstHeaderValue(response, "request-id");

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ElevenLabsApiException("empty_response", "Pusta odpowiedź z API ElevenLabs.", statusCode, requestId);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions)
                   ?? throw new ElevenLabsApiException("empty_response", "Pusta odpowiedź z API ElevenLabs.", statusCode, requestId);
        }
        catch (JsonException)
        {
            throw new ElevenLabsApiException("unexpected_response",
                "Odpowiedź API ElevenLabs ma nieoczekiwany format.", statusCode, requestId);
        }
    }

    /// <summary>Returns the response only for a success status; the caller disposes it.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (HttpRequestException e)
        {
            throw new ElevenLabsApiException("connection_failed",
                $"Nie udało się połączyć z API ElevenLabs: {e.Message}", 0);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ElevenLabsApiException("timeout", "API ElevenLabs nie odpowiedziało w wyznaczonym czasie.", 0);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            throw CreateApiException(response, await response.Content.ReadAsStringAsync(ct));
        }
    }

    private static ElevenLabsApiException CreateApiException(HttpResponseMessage response, string body)
    {
        var statusCode = (int)response.StatusCode;
        var error = ParseError(body);
        var errorCode = error.Code ?? error.LegacyStatus ?? ErrorCodeFor(statusCode);
        var requestId = error.RequestId ?? FirstHeaderValue(response, "request-id");
        var message = BuildMessage(errorCode, error.LegacyStatus, statusCode, error.Message);
        return new ElevenLabsApiException(errorCode, message, statusCode, requestId);
    }

    /// <summary>
    /// "detail" is documented as an object: code, message, request_id, and the legacy "status". The two can differ:
    /// a wrong key answers code "unauthorized" with status "invalid_api_key", so the legacy value is kept for choosing
    /// the message. Validation errors (422) can also answer "detail" as a list of { msg }, and older errors as text.
    /// </summary>
    private static (string? Code, string? LegacyStatus, string? Message, string? RequestId) ParseError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("detail", out var detail))
            {
                return default;
            }

            return detail.ValueKind switch
            {
                JsonValueKind.Object => (
                    GetString(detail, "code"),
                    GetString(detail, "status"),
                    GetString(detail, "message"),
                    GetString(detail, "request_id")),
                JsonValueKind.Array when detail.GetArrayLength() > 0 => (null, null, GetString(detail[0], "msg"), null),
                JsonValueKind.String => (null, null, detail.GetString(), null),
                _ => default
            };
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
               && element.TryGetProperty(name, out var value)
               && value.ValueKind == JsonValueKind.String
            ? NullIfBlank(value.GetString())
            : null;
    }

    private static string ErrorCodeFor(int statusCode)
    {
        return statusCode switch
        {
            400 or 422 => "bad_request",
            401 => "unauthorized",
            402 => "payment_required",
            403 => "forbidden",
            404 => "not_found",
            429 => "too_many_requests",
            >= 500 => "server_error",
            _ => $"http_{statusCode}"
        };
    }

    /// <summary>Operator-facing explanation; the API's own (trimmed, capped) message is appended when present.</summary>
    private static string BuildMessage(string errorCode, string? legacyStatus, int statusCode, string? apiMessage)
    {
        var text = MessageForCode(errorCode) ?? MessageForCode(legacyStatus) ?? MessageForStatus(statusCode);

        var detail = apiMessage?.Trim();
        if (string.IsNullOrEmpty(detail))
        {
            return text;
        }

        if (detail.Length > MaxApiMessageLength)
        {
            detail = detail[..MaxApiMessageLength] + "…";
        }

        return $"{text} ({detail})";
    }

    private static string? MessageForCode(string? code)
    {
        return code switch
        {
            "insufficient_credits" or "quota_exceeded" =>
                "Brak kredytów na koncie ElevenLabs w bieżącym okresie rozliczeniowym.",
            "voice_not_found" or "invalid_voice_id" =>
                "ElevenLabs nie zna wskazanego głosu - sprawdź identyfikator głosu.",
            "voice_access_denied" or "model_access_denied" or "paid_plan_required" or "subscription_required"
                or "feature_not_available" =>
                "Plan konta ElevenLabs nie daje dostępu do tego głosu, modelu lub funkcji.",
            "concurrent_limit_exceeded" =>
                "Za dużo równoczesnych żądań do API ElevenLabs - poczekaj na zakończenie trwających.",
            _ => null
        };
    }

    private static string MessageForStatus(int statusCode)
    {
        return statusCode switch
        {
            401 => "ElevenLabs odrzucił klucz API - klucz jest nieprawidłowy, unieważniony albo nie ma wymaganych uprawnień.",
            402 => "ElevenLabs wymaga płatności lub brakuje kredytów na koncie.",
            403 => "Konto ElevenLabs nie ma uprawnień do tej operacji.",
            404 => "ElevenLabs nie znalazł żądanego zasobu.",
            400 or 422 => "API ElevenLabs odrzuciło żądanie jako nieprawidłowe.",
            429 => "Zbyt wiele żądań do API ElevenLabs - odczekaj chwilę i ponów.",
            >= 500 => "API ElevenLabs zgłosiło błąd po swojej stronie - spróbuj ponownie za chwilę.",
            _ => $"API ElevenLabs odpowiedziało kodem {statusCode}."
        };
    }

    private static string? FirstHeaderValue(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record TextToSpeechBody(
        string Text,
        string ModelId,
        string? LanguageCode,
        ElevenLabsVoiceSettings? VoiceSettings,
        uint? Seed);
}
