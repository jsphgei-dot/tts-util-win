/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Settings;

/// <summary>Where the program keeps its voices and its settings.</summary>
public static class InstallPaths
{
    /// <summary>Environment variable that overrides the voices directory.</summary>
    public const string VoicesEnvironmentVariable = "TTSUTIL_VOICES";

    /// <summary>File beside the executable that forces portable mode.</summary>
    public const string PortableMarkerFile = "portable.txt";

    public const string ApplicationFolderName = "TtsUtilWin";

    public const string SettingsFileName = "settings.json";

    public const string VoicesFolderName = "voices";

    /// <summary>Picks the voices directory from the first source that names one.</summary>
    public static string ResolveVoicesDirectory(
        string? configured,
        string? environmentValue,
        string localAppData,
        string appDirectory,
        Func<string, bool> hasVoices)
    {
        if (!string.IsNullOrWhiteSpace(configured)) return configured!;
        if (!string.IsNullOrWhiteSpace(environmentValue)) return environmentValue!;

        var installed = Path.Combine(localAppData, ApplicationFolderName, VoicesFolderName);
        if (hasVoices(installed)) return installed;

        var beside = Path.Combine(appDirectory, VoicesFolderName);
        if (hasVoices(beside)) return beside;

        var fromSourceTree = SearchParentsForVoices(appDirectory, hasVoices);
        if (fromSourceTree is not null) return fromSourceTree;

        // Nothing exists yet: name the place a download should go.
        return IsPortable(appDirectory, File.Exists) ? beside : installed;
    }

    /// <summary>Picks the settings file: beside the executable in portable mode, otherwise roaming.</summary>
    public static string ResolveSettingsPath(string appDirectory, string roamingAppData, Func<string, bool> fileExists)
    {
        if (IsPortable(appDirectory, fileExists)) return Path.Combine(appDirectory, SettingsFileName);

        return Path.Combine(roamingAppData, ApplicationFolderName, SettingsFileName);
    }

    /// <summary>True when a portable marker or an existing settings file sits beside the executable.</summary>
    public static bool IsPortable(string appDirectory, Func<string, bool> fileExists) =>
        fileExists(Path.Combine(appDirectory, PortableMarkerFile)) ||
        fileExists(Path.Combine(appDirectory, SettingsFileName));

    /// <summary>True when the directory exists and holds at least one model folder.</summary>
    public static bool HasVoices(string directory)
    {
        try
        {
            return Directory.Exists(directory) && Directory.EnumerateDirectories(directory).Any();
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string? SearchParentsForVoices(string appDirectory, Func<string, bool> hasVoices)
    {
        var current = new DirectoryInfo(appDirectory);

        for (var depth = 0; depth < 6 && current?.Parent is not null; depth++)
        {
            current = current.Parent;
            var candidate = Path.Combine(current.FullName, VoicesFolderName);
            if (hasVoices(candidate)) return candidate;
        }

        return null;
    }
}
