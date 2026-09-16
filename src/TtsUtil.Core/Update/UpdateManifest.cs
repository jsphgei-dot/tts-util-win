/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TtsUtil.Core.Update;

/// <summary>One downloadable file in a published release.</summary>
public sealed class UpdateDownload
{
    public string Url { get; set; } = string.Empty;

    public long Bytes { get; set; }

    /// <summary>Lower case hexadecimal SHA256 of the file, checked before anything is run.</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>True when there is enough here to fetch and verify the file.</summary>
    public bool IsUsable =>
        Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && Sha256.Length == 64;
}

/// <summary>What the published manifest says the newest release is.</summary>
public sealed class UpdateManifest
{
    /// <summary>Semantic version name of the newest release, such as "0.3.0-beta".</summary>
    public string VersionName { get; set; } = string.Empty;

    /// <summary>Monotonic version code, which is what decides whether a build is newer.</summary>
    public int VersionCode { get; set; }

    /// <summary>The release page, which is where a portable copy sends the reader.</summary>
    public string ReleaseUrl { get; set; } = string.Empty;

    public UpdateDownload? Setup { get; set; }

    public UpdateDownload? Portable { get; set; }

    /// <summary>What changed in that release, shown on the Updates tab before anything is fetched.</summary>
    public IReadOnlyList<string> Notes { get; set; } = Array.Empty<string>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Reads a manifest, or null when the text is not one. Never throws on bad input.</summary>
    public static UpdateManifest? Parse(string json)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions);
            if (manifest is null || manifest.VersionCode <= 0) return null;
            if (string.IsNullOrWhiteSpace(manifest.VersionName)) return null;

            manifest.Notes = manifest.Notes?.Where(note => !string.IsNullOrWhiteSpace(note)).ToList()
                ?? (IReadOnlyList<string>)Array.Empty<string>();

            return manifest;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>True when this release is later than the running build.</summary>
    public bool IsNewerThan(int runningVersionCode) => VersionCode > runningVersionCode;
}
