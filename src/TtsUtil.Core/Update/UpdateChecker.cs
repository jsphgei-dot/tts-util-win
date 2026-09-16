/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Update;

/// <summary>Fetches the published manifest. Every failure is silent, since this is a courtesy.</summary>
public sealed class UpdateChecker : IDisposable
{
    /// <summary>How long a copy waits before asking again, so a restart is not a request.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly HttpClient _client;
    private readonly bool _ownsClient;

    public UpdateChecker(HttpClient? client = null)
    {
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _ownsClient = client is null;
    }

    /// <summary>True when the copy is due a check, given when it last managed one.</summary>
    public static bool IsDue(bool enabled, DateTime? lastCheckUtc, DateTime nowUtc) =>
        enabled && (lastCheckUtc is null || nowUtc - lastCheckUtc.Value >= Interval);

    /// <summary>Reads the manifest, or null when it cannot be had or does not parse.</summary>
    public async Task<UpdateManifest?> FetchAsync(string manifestUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, manifestUrl);

            // GitHub turns away callers with no user agent, and the name is all that is sent.
            request.Headers.UserAgent.ParseAdd("TtsUtilWin/" + AppVersion.Name);

            using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return UpdateManifest.Parse(json);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_ownsClient) _client.Dispose();
    }
}
