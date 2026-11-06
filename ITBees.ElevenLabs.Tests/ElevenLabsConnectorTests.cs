using System.Text.Json;
using ITBees.ElevenLabs.Models;
using ITBees.ElevenLabs.Services;

namespace ITBees.ElevenLabs.Tests;

public class ElevenLabsConnectorTests
{
    private const string ApiKey = "test-key";
    private const string VoiceId = "BFd5oBc2DDna33pSi4Gf";

    private static readonly byte[] SampleAudio = { 0x49, 0x44, 0x33, 0x04 };

    private static (ElevenLabsConnector connector, FakeElevenLabsHandler handler) Create(
        Func<CapturedRequest, HttpResponseMessage> responder, string? apiKey = ApiKey)
    {
        var handler = new FakeElevenLabsHandler(responder);
        return (new ElevenLabsConnector(new HttpClient(handler), new FakeApiKeyProvider(apiKey)), handler);
    }

    private static ElevenLabsSpeechRequest SpeechRequest(ElevenLabsModel model = ElevenLabsModel.V4)
    {
        return new ElevenLabsSpeechRequest
        {
            Text = "Proszę wyjechać z parkingu.",
            VoiceId = VoiceId,
            LanguageCode = "pl",
            Model = model
        };
    }

    [Fact]
    public async Task TextToSpeech_PostsTheRequestWithDefaults_AndReturnsTheAudio()
    {
        var (connector, handler) = Create(_ =>
        {
            var response = FakeElevenLabsHandler.Audio(SampleAudio);
            response.Headers.Add("request-id", "req-1");
            response.Headers.Add("character-cost", "27");
            return response;
        });

        var result = await connector.TextToSpeechAsync(SpeechRequest());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"https://api.elevenlabs.io/v1/text-to-speech/{VoiceId}?output_format=mp3_44100_128", request.Url);
        Assert.Equal(ApiKey, request.ApiKey);
        Assert.Equal("application/json", request.ContentType);

        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("Proszę wyjechać z parkingu.", body.RootElement.GetProperty("text").GetString());
        Assert.Equal("eleven_v4", body.RootElement.GetProperty("model_id").GetString());
        Assert.Equal("pl", body.RootElement.GetProperty("language_code").GetString());
        Assert.False(body.RootElement.TryGetProperty("voice_settings", out _));
        Assert.False(body.RootElement.TryGetProperty("seed", out _));

        Assert.Equal(SampleAudio, result.Audio);
        Assert.Equal("audio/mpeg", result.ContentType);
        Assert.Equal("mp3", result.FileExtension);
        Assert.Equal("req-1", result.RequestId);
        Assert.Equal(27, result.CharacterCost);
    }

    [Fact]
    public async Task TextToSpeech_SendsVoiceSettingsSeedAndFormat()
    {
        var (connector, handler) = Create(_ => FakeElevenLabsHandler.Audio(SampleAudio, "audio/wav"));
        var speechRequest = SpeechRequest();
        speechRequest.OutputFormat = ElevenLabsOutputFormat.Wav_24000;
        speechRequest.Seed = 4294967295;
        speechRequest.VoiceSettings = new ElevenLabsVoiceSettings { Stability = 0.8, UseSpeakerBoost = false };

        var result = await connector.TextToSpeechAsync(speechRequest);

        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("?output_format=wav_24000", request.Url);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal(4294967295, body.RootElement.GetProperty("seed").GetInt64());
        var settings = body.RootElement.GetProperty("voice_settings");
        Assert.Equal(0.8, settings.GetProperty("stability").GetDouble());
        Assert.False(settings.GetProperty("use_speaker_boost").GetBoolean());
        Assert.False(settings.TryGetProperty("similarity_boost", out _));
        Assert.Equal("wav", result.FileExtension);
        Assert.Null(result.RequestId);
        Assert.Null(result.CharacterCost);
    }

    [Fact]
    public async Task TextToSpeech_LeavesTheLanguageCodeOutForMultilingualV2()
    {
        var (connector, handler) = Create(_ => FakeElevenLabsHandler.Audio(SampleAudio));

        await connector.TextToSpeechAsync(SpeechRequest(ElevenLabsModel.MultilingualV2));

        using var body = JsonDocument.Parse(Assert.Single(handler.Requests).Body!);
        Assert.Equal("eleven_multilingual_v2", body.RootElement.GetProperty("model_id").GetString());
        Assert.False(body.RootElement.TryGetProperty("language_code", out _));
    }

    [Fact]
    public async Task TextToSpeech_EscapesTheVoiceId()
    {
        var (connector, handler) = Create(_ => FakeElevenLabsHandler.Audio(SampleAudio));
        var speechRequest = SpeechRequest();
        speechRequest.VoiceId = "../../v1/user?x=1";

        await connector.TextToSpeechAsync(speechRequest);

        Assert.Contains("/v1/text-to-speech/..%2F..%2Fv1%2Fuser%3Fx%3D1?output_format=", Assert.Single(handler.Requests).Url);
    }

    [Theory]
    [InlineData("", "Tekst", "missing_voice_id")]
    [InlineData(VoiceId, "  ", "empty_text")]
    public async Task TextToSpeech_RefusesAnIncompleteRequestWithoutCallingTheApi(string voiceId, string text, string errorCode)
    {
        var (connector, handler) = Create(_ => FakeElevenLabsHandler.Audio(SampleAudio));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() =>
            connector.TextToSpeechAsync(new ElevenLabsSpeechRequest { VoiceId = voiceId, Text = text }));

        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task TextToSpeech_ThrowsOnEmptyAudio()
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Audio(Array.Empty<byte>()));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.TextToSpeechAsync(SpeechRequest()));

        Assert.Equal("empty_response", exception.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnyCall_WithoutAKey_ThrowsWithoutCallingTheApi(string? apiKey)
    {
        var (connector, handler) = Create(_ => FakeElevenLabsHandler.Audio(SampleAudio), apiKey);

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.GetSubscriptionAsync());

        Assert.Equal("missing_api_key", exception.ErrorCode);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task AnError_UsesTheDocumentedDetailObject()
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(402,
            """{"detail":{"type":"payment_required","code":"insufficient_credits","message":"Not enough credits.","status":"insufficient_credits","request_id":"req-2"}}"""));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.TextToSpeechAsync(SpeechRequest()));

        Assert.Equal("insufficient_credits", exception.ErrorCode);
        Assert.Equal(402, exception.StatusCode);
        Assert.Equal("req-2", exception.RequestId);
        Assert.StartsWith("Brak kredytów", exception.Message);
        Assert.EndsWith("(Not enough credits.)", exception.Message);
    }

    [Fact]
    public async Task AnError_FallsBackToTheLegacyStatusField()
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(401,
            """{"detail":{"status":"quota_exceeded","message":"Over the limit."}}"""));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.TextToSpeechAsync(SpeechRequest()));

        Assert.Equal("quota_exceeded", exception.ErrorCode);
        Assert.StartsWith("Brak kredytów", exception.Message);
    }

    /// <summary>The body answered by the real API for a wrong key.</summary>
    [Fact]
    public async Task AnError_ForAWrongKey_ExplainsTheKey()
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(401,
            """{"detail":{"type":"authentication_error","code":"unauthorized","message":"Invalid API key","status":"invalid_api_key","request_id":"req-3"}}"""));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.GetSubscriptionAsync());

        Assert.Equal("unauthorized", exception.ErrorCode);
        Assert.Equal("req-3", exception.RequestId);
        Assert.StartsWith("ElevenLabs odrzucił klucz API", exception.Message);
        Assert.EndsWith("(Invalid API key)", exception.Message);
    }

    [Fact]
    public async Task AnError_WithAGenericCode_TakesTheMessageFromTheLegacyStatus()
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(401,
            """{"detail":{"code":"unauthorized","status":"quota_exceeded","message":"Over the limit."}}"""));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.TextToSpeechAsync(SpeechRequest()));

        Assert.Equal("unauthorized", exception.ErrorCode);
        Assert.StartsWith("Brak kredytów", exception.Message);
    }

    [Fact]
    public async Task AnError_ReadsAValidationList()
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(422,
            """{"detail":[{"loc":["body","text"],"msg":"Field required","type":"missing"}]}"""));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.TextToSpeechAsync(SpeechRequest()));

        Assert.Equal("bad_request", exception.ErrorCode);
        Assert.Contains("Field required", exception.Message);
    }

    [Fact]
    public async Task AnError_ReadsPlainTextDetail()
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(404, """{"detail":"Not Found"}"""));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.GetVoicesAsync());

        Assert.Equal("not_found", exception.ErrorCode);
        Assert.Contains("Not Found", exception.Message);
    }

    [Theory]
    [InlineData(401, "unauthorized")]
    [InlineData(429, "too_many_requests")]
    [InlineData(503, "server_error")]
    [InlineData(307, "http_307")]
    public async Task AnError_WithoutAJsonBody_FollowsTheStatus(int status, string errorCode)
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(status, "<html>gateway</html>"));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.GetSubscriptionAsync());

        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(status, exception.StatusCode);
    }

    [Fact]
    public async Task AnError_CapsTheApiMessage()
    {
        var longMessage = new string('x', 1000);
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(400,
            """{"detail":{"code":"invalid_parameters","message":"MESSAGE"}}""".Replace("MESSAGE", longMessage)));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.TextToSpeechAsync(SpeechRequest()));

        Assert.DoesNotContain(new string('x', 301), exception.Message);
        Assert.Contains(new string('x', 300) + "…", exception.Message);
    }

    [Fact]
    public async Task NoConnection_IsReportedAsConnectionFailed()
    {
        var (connector, _) = Create(_ => throw new HttpRequestException("No such host."));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.GetSubscriptionAsync());

        Assert.Equal("connection_failed", exception.ErrorCode);
        Assert.Equal(0, exception.StatusCode);
    }

    [Fact]
    public async Task AClientTimeout_IsReportedAsTimeout()
    {
        var httpClient = new HttpClient(new HangingHandler()) { Timeout = TimeSpan.FromMilliseconds(50) };
        var connector = new ElevenLabsConnector(httpClient, new FakeApiKeyProvider(ApiKey));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.GetSubscriptionAsync());

        Assert.Equal("timeout", exception.ErrorCode);
    }

    [Fact]
    public async Task ACallerCancellation_IsNotDressedUpAsAnApiError()
    {
        using var cancellation = new CancellationTokenSource();
        var connector = new ElevenLabsConnector(new HttpClient(new HangingHandler()), new FakeApiKeyProvider(ApiKey));

        var call = connector.GetSubscriptionAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
    }

    [Fact]
    public async Task GetVoices_WithoutAQuery_CallsThePlainAddress()
    {
        var (connector, handler) = Create(_ => FakeElevenLabsHandler.Json(200, """{"voices":[],"has_more":false}"""));

        var page = await connector.GetVoicesAsync();

        Assert.Equal("https://api.elevenlabs.io/v2/voices", Assert.Single(handler.Requests).Url);
        Assert.Empty(page.Voices);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task GetVoices_SendsEscapedFilters_AndRepeatsLanguages()
    {
        var (connector, handler) = Create(_ => FakeElevenLabsHandler.Json(200, """{"voices":[],"has_more":false}"""));

        await connector.GetVoicesAsync(new ElevenLabsVoicesQuery
        {
            Search = "calm anchor&x",
            VoiceType = "non-default",
            Category = "generated",
            Languages = new List<string> { "pl", "de" },
            PageSize = 100,
            NextPageToken = "abc"
        });

        Assert.Equal(
            "https://api.elevenlabs.io/v2/voices?search=calm%20anchor%26x&voice_type=non-default&category=generated" +
            "&language=pl&language=de&page_size=100&next_page_token=abc",
            Assert.Single(handler.Requests).Url);
    }

    [Fact]
    public async Task GetVoices_ParsesTheFieldsUsedForChoosingAVoice()
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(200, """
            {
              "voices": [{
                "voice_id": "v1",
                "name": "Alicia",
                "samples": null,
                "category": "generated",
                "description": "Polished anchor",
                "preview_url": "https://example.com/v1.mp3",
                "labels": { "accent": "british", "gender": "female" },
                "settings": { "stability": 1, "speed": 1 },
                "sharing": { "status": "enabled", "notice_period": 30, "disable_at_unix": 1714204800 },
                "verified_languages": [
                  { "language": "pl", "model_id": "eleven_v4", "accent": "standard", "locale": "pl-PL", "preview_url": "https://example.com/v1_pl.mp3" }
                ],
                "is_legacy": false
              }],
              "has_more": true,
              "total_count": 12,
              "next_page_token": "next"
            }
            """));

        var page = await connector.GetVoicesAsync();

        var voice = Assert.Single(page.Voices);
        Assert.Equal("v1", voice.VoiceId);
        Assert.Equal("Alicia", voice.Name);
        Assert.Equal("generated", voice.Category);
        Assert.Equal("https://example.com/v1.mp3", voice.PreviewUrl);
        Assert.Equal("british", voice.Labels!["accent"]);
        Assert.Equal(30, voice.Sharing!.NoticePeriod);
        Assert.Equal(1714204800, voice.Sharing.DisableAtUnix);
        Assert.False(voice.IsLegacy);
        var language = Assert.Single(voice.VerifiedLanguages!);
        Assert.Equal("pl", language.Language);
        Assert.Equal("eleven_v4", language.ModelId);
        Assert.Equal("pl-PL", language.Locale);
        Assert.Equal("https://example.com/v1_pl.mp3", language.PreviewUrl);
        Assert.True(page.HasMore);
        Assert.Equal("next", page.NextPageToken);
    }

    [Fact]
    public async Task GetSubscription_ParsesTheCredits_IgnoringOtherFields()
    {
        var (connector, handler) = Create(_ => FakeElevenLabsHandler.Json(200, """
            {
              "tier": "starter",
              "character_count": 1000,
              "character_limit": 30000,
              "max_credit_limit_extension": "unlimited",
              "status": "active",
              "next_character_count_reset_unix": 1738356858
            }
            """));

        var subscription = await connector.GetSubscriptionAsync();

        Assert.Equal("https://api.elevenlabs.io/v1/user/subscription", Assert.Single(handler.Requests).Url);
        Assert.Equal("starter", subscription.Tier);
        Assert.Equal("active", subscription.Status);
        Assert.Equal(1000, subscription.CharacterCount);
        Assert.Equal(30000, subscription.CharacterLimit);
        Assert.Equal(29000, subscription.RemainingCharacters);
        Assert.Equal(1738356858, subscription.NextCharacterCountResetUnix);
    }

    [Theory]
    [InlineData("", "empty_response")]
    [InlineData("null", "empty_response")]
    [InlineData("<html></html>", "unexpected_response")]
    public async Task ASuccessStatusWithAnUnusableBody_Throws(string responseBody, string errorCode)
    {
        var (connector, _) = Create(_ => FakeElevenLabsHandler.Json(200, responseBody));

        var exception = await Assert.ThrowsAsync<ElevenLabsApiException>(() => connector.GetSubscriptionAsync());

        Assert.Equal(errorCode, exception.ErrorCode);
    }
}
