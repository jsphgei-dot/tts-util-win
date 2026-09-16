/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.IO.Compression;
using System.Security.Cryptography;

namespace TtsUtil.Core.Update;

/// <summary>How far along the fetch of an update is.</summary>
public readonly struct UpdateProgress
{
    public UpdateProgress(long bytesReceived, long totalBytes)
    {
        BytesReceived = bytesReceived;
        TotalBytes = totalBytes;
    }

    public long BytesReceived { get; }

    public long TotalBytes { get; }

    public double Fraction => TotalBytes > 0 ? (double)BytesReceived / TotalBytes : 0;
}

/// <summary>Why fetching an update stopped short, so the window can say something useful.</summary>
public enum UpdateFetchResult
{
    Ready,
    Cancelled,
    DownloadFailed,
    HashMismatch,
    NoSetupInside,
}

/// <summary>
/// Fetches the setup program for a newer release, checks it against the published hash and
/// unpacks it. Running it and standing aside is the window's job, not this class's.
/// </summary>
public sealed class UpdateInstaller : IDisposable
{
    private const int BufferSize = 81920;

    private readonly HttpClient _client;
    private readonly bool _ownsClient;

    public UpdateInstaller(HttpClient? client = null)
    {
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        _ownsClient = client is null;
    }

    /// <summary>The setup program, ready to run, or the reason there is not one.</summary>
    public async Task<(UpdateFetchResult Result, string? SetupPath)> FetchAsync(UpdateDownload download,
        string workingDirectory, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        if (!download.IsUsable) return (UpdateFetchResult.DownloadFailed, null);

        Directory.CreateDirectory(workingDirectory);
        var archive = Path.Combine(workingDirectory, "update.zip");

        try
        {
            await DownloadAsync(download.Url, archive, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return (UpdateFetchResult.Cancelled, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            return (UpdateFetchResult.DownloadFailed, null);
        }

        // An executable is about to be run, so the published hash decides whether it is the one.
        var actual = await HashAsync(archive, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(actual, download.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            TryDelete(archive);
            return (UpdateFetchResult.HashMismatch, null);
        }

        var unpacked = Path.Combine(workingDirectory, "unpacked");

        try
        {
            if (Directory.Exists(unpacked)) Directory.Delete(unpacked, recursive: true);
            ZipFile.ExtractToDirectory(archive, unpacked);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException
                                              or UnauthorizedAccessException)
        {
            return (UpdateFetchResult.DownloadFailed, null);
        }

        var setup = FindSetup(unpacked);
        return setup is null
            ? (UpdateFetchResult.NoSetupInside, null)
            : (UpdateFetchResult.Ready, setup);
    }

    /// <summary>The setup program inside an unpacked release, whatever the version in its name.</summary>
    public static string? FindSetup(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*setup*.exe", SearchOption.AllDirectories)
                .OrderBy(path => path.Length)
                .FirstOrDefault()
            : null;

    private async Task DownloadAsync(string url, string destinationPath, IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
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
            progress?.Report(new UpdateProgress(received, total));
        }
    }

    private static async Task<string> HashAsync(string path, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize,
            useAsync: true);

        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }

    public void Dispose()
    {
        if (_ownsClient) _client.Dispose();
    }
}
