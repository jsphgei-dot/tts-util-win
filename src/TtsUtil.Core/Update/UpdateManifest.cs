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

/// <summary>The pair of downloads published for one architecture.</summary>
public sealed class UpdateBuild
{
    public UpdateDownload? Setup { get; set; }

    public UpdateDownload? Portable { get; set; }
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

    /// <summary>The x64 downloads, which is where a copy built before 0.13.0 looks.</summary>
    public UpdateDownload? Setup { get; set; }

    public UpdateDownload? Portable { get; set; }

    /// <summary>Every published architecture, keyed as <see cref="HostArchitecture"/> spells them.</summary>
    public IReadOnlyDictionary<string, UpdateBuild>? Architectures { get; set; }

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

            if (manifest.Architectures is { Count: > 0 } published)
            {
                manifest.Architectures = new Dictionary<string, UpdateBuild>(published,
                    StringComparer.OrdinalIgnoreCase);
            }

            return manifest;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The setup program for one architecture, or null when the release has none.</summary>
    public UpdateDownload? SetupFor(string architecture) => BuildFor(architecture)?.Setup;

    /// <summary>The portable archive for one architecture, on the terms of <see cref="SetupFor"/>.</summary>
    public UpdateDownload? PortableFor(string architecture) => BuildFor(architecture)?.Portable;

    /// A manifest naming no architectures was written before 0.13.0, and its downloads are x64.
    private UpdateBuild? BuildFor(string architecture)
    {
        if (Architectures is not null)
        {
            return Architectures.TryGetValue(architecture, out var build) ? build : null;
        }

        return architecture == HostArchitecture.X64
            ? new UpdateBuild { Setup = Setup, Portable = Portable }
            : null;
    }

    /// <summary>True when this release is later than the running build.</summary>
    public bool IsNewerThan(int runningVersionCode) => VersionCode > runningVersionCode;
}
