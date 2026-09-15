/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Diagnostics;

namespace TtsUtil.Core.Tts;

/// <summary>Unpacks a tar.bz2 with the tar that ships with Windows 10 and later.</summary>
public sealed class TarArchiveExtractor : IArchiveExtractor
{
    private readonly string _tarPath;

    public TarArchiveExtractor(string? tarPath = null)
    {
        _tarPath = tarPath ?? DefaultTarPath();
    }

    public static string DefaultTarPath()
    {
        var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var candidate = Path.Combine(system, "tar.exe");
        return File.Exists(candidate) ? candidate : "tar";
    }

    public async Task ExtractAsync(string archivePath, string destinationDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);

        var info = new ProcessStartInfo(_tarPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        info.ArgumentList.Add("-xf");
        info.ArgumentList.Add(archivePath);
        info.ArgumentList.Add("-C");
        info.ArgumentList.Add(destinationDirectory);

        using var process = Process.Start(info)
                            ?? throw new InvalidOperationException("tar could not be started.");

        var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"tar failed with exit code {process.ExitCode}. {error}".TrimEnd());
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException)
        {
        }
    }
}
