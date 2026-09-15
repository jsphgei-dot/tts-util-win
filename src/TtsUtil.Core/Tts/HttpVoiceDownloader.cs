/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Tts;

/// <summary>Downloads a voice archive over HTTPS, reporting bytes as they arrive.</summary>
public sealed class HttpVoiceDownloader : IVoiceDownloader, IDisposable
{
    private const int BufferSize = 81920;

    private readonly HttpClient _client;
    private readonly bool _ownsClient;

    public HttpVoiceDownloader(HttpClient? client = null)
    {
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        _ownsClient = client is null;
    }

    public async Task DownloadAsync(string url, string destinationPath, IProgress<VoiceInstallProgress>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        using var response = await _client
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? 0;
        var received = 0L;
        var buffer = new byte[BufferSize];

        using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None,
            BufferSize, useAsync: true);

        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                .ConfigureAwait(false);

            if (read == 0) break;

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            received += read;

            progress?.Report(new VoiceInstallProgress
            {
                Phase = VoiceInstallPhase.Downloading,
                BytesReceived = received,
                TotalBytes = total,
            });
        }
    }

    public void Dispose()
    {
        if (_ownsClient) _client.Dispose();
    }
}
