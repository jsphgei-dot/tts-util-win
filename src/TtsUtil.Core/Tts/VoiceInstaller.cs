/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Tts;

public enum VoiceInstallPhase
{
    Downloading,
    Extracting,
    Finished,
}

/// <summary>Progress of one voice installation.</summary>
public sealed class VoiceInstallProgress
{
    public VoiceInstallPhase Phase { get; init; }

    public long BytesReceived { get; init; }

    public long TotalBytes { get; init; }

    public int Percent => TotalBytes <= 0 ? 0 : (int)(BytesReceived * 100 / TotalBytes);
}

/// <summary>Fetches an archive to a local file.</summary>
public interface IVoiceDownloader
{
    Task DownloadAsync(string url, string destinationPath, IProgress<VoiceInstallProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>Unpacks an archive into a directory.</summary>
public interface IArchiveExtractor
{
    Task ExtractAsync(string archivePath, string destinationDirectory, CancellationToken cancellationToken);
}

/// <summary>Downloads and unpacks voice models into the voices directory.</summary>
public sealed class VoiceInstaller
{
    private readonly IVoiceDownloader _downloader;
    private readonly IArchiveExtractor _extractor;
    private readonly string _temporaryDirectory;

    public VoiceInstaller(IVoiceDownloader downloader, IArchiveExtractor extractor, string? temporaryDirectory = null)
    {
        _downloader = downloader;
        _extractor = extractor;
        _temporaryDirectory = temporaryDirectory ?? Path.GetTempPath();
    }

    /// <summary>Picks the first directory that can be written, so a Program Files install still works.</summary>
    public static string ChooseTargetDirectory(string preferred, string fallback, Func<string, bool> canWrite) =>
        canWrite(preferred) ? preferred : fallback;

    public static bool IsInstalled(DownloadableVoice voice, string voicesDirectory) =>
        VoiceCatalog.TryLoad(Path.Combine(voicesDirectory, voice.Id)) is not null;

    /// <summary>True when the directory exists and takes a file, or can be created.</summary>
    public static bool CanWrite(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probe = Path.Combine(directory, ".write-probe");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException
                                      or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Downloads and unpacks one voice, replacing any partial copy. Returns its directory.</summary>
    public async Task<string> InstallAsync(
        DownloadableVoice voice,
        string voicesDirectory,
        IProgress<VoiceInstallProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(voicesDirectory);

        var target = Path.Combine(voicesDirectory, voice.Id);
        var archive = Path.Combine(_temporaryDirectory, voice.ArchiveFileName);

        if (Directory.Exists(target)) Directory.Delete(target, recursive: true);

        try
        {
            await _downloader.DownloadAsync(DownloadableVoices.ArchiveUrl(voice), archive, progress, cancellationToken)
                .ConfigureAwait(false);

            progress?.Report(new VoiceInstallProgress { Phase = VoiceInstallPhase.Extracting });
            await _extractor.ExtractAsync(archive, voicesDirectory, cancellationToken).ConfigureAwait(false);

            if (VoiceCatalog.TryLoad(target) is null)
            {
                Remove(voice, voicesDirectory);
                throw new InvalidOperationException(
                    $"{voice.Id} unpacked without a usable model. The download may be incomplete.");
            }

            progress?.Report(new VoiceInstallProgress { Phase = VoiceInstallPhase.Finished });
            return target;
        }
        catch
        {
            Remove(voice, voicesDirectory);
            throw;
        }
        finally
        {
            TryDelete(archive);
        }
    }

    /// <summary>Deletes an installed voice. Silent when it is not there.</summary>
    public static void Remove(DownloadableVoice voice, string voicesDirectory) =>
        Remove(voice.Id, voicesDirectory);

    public static void Remove(string voiceId, string voicesDirectory)
    {
        var target = Path.Combine(voicesDirectory, voiceId);

        try
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}
