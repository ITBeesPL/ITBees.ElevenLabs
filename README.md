# ITBees.ElevenLabs

Connector to the [ElevenLabs API](https://elevenlabs.io/docs/api-reference/introduction), mainly for generating
voice messages that the host application saves and plays later. No database, no controllers: the host decides where
the audio and the settings live.

| Method of `IElevenLabsConnector` | API call | Purpose |
|---|---|---|
| `TextToSpeechAsync` | `POST /v1/text-to-speech/{voice_id}` | Generates the speech and returns the whole audio file. Uses credits |
| `GetVoicesAsync` | `GET /v2/voices` | One page of the voices of the account, with samples and verified languages |
| `GetSubscriptionAsync` | `GET /v1/user/subscription` | Plan and credits used. Generates nothing, so it also checks the key |

## Host registration

```csharp
services.AddElevenLabs();
```

The key is read before every call from the `ElevenLabsApiKey` platform setting (`IPlatformSettingsService`, usually
`config.json`), the same way `ITBees.ChatGpt` reads `ChatGptApiKey`. A missing key does not break the start of the
application; the call throws `missing_api_key`.

To keep the key elsewhere, e.g. in a database row edited in the admin panel, register your own provider:

```csharp
services.AddTransient<IElevenLabsApiKeyProvider, MyDatabaseApiKeyProvider>();
services.AddElevenLabs();
```

A key restricted in the ElevenLabs panel needs access to Text to Speech, and read access to Voices and User for the
other two methods.

## Generating a message

```csharp
var speech = await elevenLabsConnector.TextToSpeechAsync(new ElevenLabsSpeechRequest
{
    Text = "Proszę wyjechać z parkingu.",
    VoiceId = voiceId,
    LanguageCode = "pl"
}, ct);

await File.WriteAllBytesAsync($"exit_pl.{speech.FileExtension}", speech.Audio, ct);
```

The defaults are `ElevenLabsModel.V4` and `ElevenLabsOutputFormat.Mp3_44100_128`. All voices support multiple
languages, so the same `VoiceId` gives one voice across the language versions, though a voice may keep the accent it
was made with. `LanguageCode` is not sent for `ElevenLabsModel.MultilingualV2`, because the API does not support it
there.

Keep the text, `VoiceId`, model and `Seed` next to the saved file if the message may need regenerating. The same seed
repeats the audio only on a best-effort basis.

## Errors

Calls throw `ElevenLabsApiException` with `ErrorCode`, `StatusCode` and `RequestId`. The message is in Polish and can
be shown to the operator as it is. `ErrorCode` is the code answered by the API (e.g. `unauthorized` for a wrong key,
`insufficient_credits`, `voice_not_found`, `concurrent_limit_exceeded`), otherwise a code following the HTTP status.
Calls that got no response use `connection_failed` or `timeout`. Nothing is retried automatically.

## Plans and voices

Checked in the ElevenLabs documentation in November 2026; check again before relying on it.

- **Commercial use needs a paid plan.** Audio generated on the Free plan cannot be used commercially. Audio generated
  during a paid subscription keeps its commercial license after the subscription ends.
- **Do not build on the Default voices.** They are available only to accounts created before March 2026 and expire on
  31 December 2026. ElevenLabs lists replacement voices you can keep using.
- **Voice Library voices are not available over the API on the Free plan.** On paid plans a shared voice can have a
  notice period (`ElevenLabsVoice.Sharing`). Saved audio files are not affected, only generating new ones.

## Releasing

A merge to `master` builds and publishes the NuGet package to the ITBees feed
(`https://nugets.itbees.pl/v3/index.json`). The build assigns the package version, so `<Version>` in the csproj only
matters for a local `dotnet pack`.
