using System.Net;
using System.Text;
using ITBees.ElevenLabs.Interfaces;
using ITBees.Interfaces.Platforms;
using ITBees.Models.EmailAccounts;
using Environment = ITBees.Interfaces.Platforms.Environment;

namespace ITBees.ElevenLabs.Tests;

/// <summary>Records every request and answers with whatever the test's responder returns.</summary>
internal sealed class FakeElevenLabsHandler : HttpMessageHandler
{
    private readonly Func<CapturedRequest, HttpResponseMessage> _responder;

    public FakeElevenLabsHandler(Func<CapturedRequest, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public List<CapturedRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var apiKey = request.Headers.TryGetValues("xi-api-key", out var values) ? values.Single() : null;
        var captured = new CapturedRequest(request.Method, request.RequestUri!.AbsoluteUri, apiKey, body,
            request.Content?.Headers.ContentType?.MediaType);
        Requests.Add(captured);
        return _responder(captured);
    }

    public static HttpResponseMessage Json(int status, string json)
    {
        return new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public static HttpResponseMessage Audio(byte[] audio, string contentType = "audio/mpeg")
    {
        var content = new ByteArrayContent(audio);
        content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }
}

internal sealed record CapturedRequest(HttpMethod Method, string Url, string? ApiKey, string? Body, string? ContentType);

/// <summary>Never answers, until the request is cancelled.</summary>
internal sealed class HangingHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        throw new InvalidOperationException("Unreachable.");
    }
}

internal sealed class FakeApiKeyProvider : IElevenLabsApiKeyProvider
{
    private readonly string? _apiKey;

    public FakeApiKeyProvider(string? apiKey)
    {
        _apiKey = apiKey;
    }

    public string? GetApiKey()
    {
        return _apiKey;
    }
}

internal sealed class FakePlatformSettingsService : IPlatformSettingsService
{
    public Dictionary<string, string> Settings { get; } = new();

    public string GetSetting(string key)
    {
        return Settings.TryGetValue(key, out var value) ? value : null!;
    }

    public List<string> GetPlatformDebugEmails()
    {
        throw new NotSupportedException();
    }

    public Environment GetCurrentEnvironment()
    {
        throw new NotSupportedException();
    }

    public EmailAccount GetPlatformDefaultEmailAccount()
    {
        throw new NotSupportedException();
    }
}
