/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Reflection;

namespace TtsUtil.Core;

/// <summary>The running build's version name and version code.</summary>
public static class AppVersion
{
    private static readonly Assembly Assembly = typeof(AppVersion).Assembly;

    /// <summary>Semantic version name, such as "0.1.0-alpha".</summary>
    public static string Name { get; } = ReadName();

    /// <summary>Monotonic build number, one per release.</summary>
    public static int Code { get; } = ReadCode();

    /// <summary>Version name and code together, such as "0.1.0-alpha (build 1)".</summary>
    public static string Display => $"{Name} (build {Code})";

    private static string ReadName()
    {
        var informational = Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informational)) return Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        // Strip the source revision the SDK appends after a plus sign.
        var plus = informational.IndexOf('+');
        return plus >= 0 ? informational[..plus] : informational;
    }

    private static int ReadCode()
    {
        foreach (var attribute in Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == "VersionCode" && int.TryParse(attribute.Value, out var code)) return code;
        }

        return 0;
    }
}
