/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Tts;

/// <summary>A link to a voice model archive somewhere other than the built in list, which can be
/// unpacked into the voices directory like any other.</summary>
public static class VoiceArchiveLink
{
    /// <summary>The archive endings the tar that ships with Windows can unpack.</summary>
    public static IReadOnlyList<string> Endings { get; } =
        new[] { ".tar.bz2", ".tar.gz", ".tar.xz", ".tar", ".tgz", ".zip" };

    /// <summary>The file a link points at, or null when it is not an https link to an archive.</summary>
    public static string? FileNameFrom(string? url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttps) return null;

        var file = Uri.UnescapeDataString(uri.Segments[^1]).Trim('/');
        return Endings.Any(ending => file.EndsWith(ending, StringComparison.OrdinalIgnoreCase)) ? file : null;
    }

    /// <summary>The folder such an archive is expected to unpack into.</summary>
    public static string? NameFrom(string? url)
    {
        if (FileNameFrom(url) is not string file) return null;

        var ending = Endings.First(e => file.EndsWith(e, StringComparison.OrdinalIgnoreCase));
        var name = file[..^ending.Length];
        return name.Length == 0 ? null : name;
    }
}
