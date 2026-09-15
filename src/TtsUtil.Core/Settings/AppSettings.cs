/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Text.Json;
using System.Text.Json.Serialization;
using TtsUtil.Core.Text;

namespace TtsUtil.Core.Settings;

/// <summary>User settings, stored beside the executable when that is writable.</summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string? VoicesDirectory { get; set; }

    public string? LastVoiceName { get; set; }

    public int SpeakerId { get; set; }

    public float Speed { get; set; } = 1.0f;

    public int NumThreads { get; set; } = 2;

    public int SilenceLineEndingMs { get; set; } = 200;

    public int SilenceSentenceMs { get; set; }

    public int SilenceQuestionMs { get; set; }

    public int SilenceExclamationMs { get; set; }

    public bool ScaleSilenceToRate { get; set; }

    public bool FilterHashes { get; set; }

    public bool FilterWebLinks { get; set; }

    public bool FilterMailToLinks { get; set; }

    public bool ReadAsYouType { get; set; }

    public string? OutputDirectory { get; set; }

    public int MaxChunkLength { get; set; } = 2000;

    [JsonIgnore]
    public string ResolvedVoicesDirectory => InstallPaths.ResolveVoicesDirectory(
        VoicesDirectory,
        Environment.GetEnvironmentVariable(InstallPaths.VoicesEnvironmentVariable),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppDirectory,
        InstallPaths.HasVoices);

    [JsonIgnore]
    public string ResolvedOutputDirectory =>
        string.IsNullOrWhiteSpace(OutputDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            : OutputDirectory!;

    /// <summary>Directory holding the running executable.</summary>
    public static string AppDirectory =>
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    /// <summary>True when the program is running from a portable folder.</summary>
    public static bool IsPortable => InstallPaths.IsPortable(AppDirectory, File.Exists);

    public static string SettingsPath
    {
        get
        {
            var path = InstallPaths.ResolveSettingsPath(
                AppDirectory,
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                File.Exists);

            var directory = Path.GetDirectoryName(path);
            if (directory is not null) Directory.CreateDirectory(directory);
            return path;
        }
    }

    /// <summary>Where this instance was loaded from, and where Save writes back to.</summary>
    [JsonIgnore]
    public string? SourcePath { get; set; }

    public static AppSettings Load() => LoadFrom(SettingsPath);

    public static AppSettings LoadFrom(string path)
    {
        try
        {
            var settings = File.Exists(path)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new AppSettings()
                : new AppSettings();
            settings.SourcePath = path;
            return settings;
        }
        catch (Exception)
        {
            return new AppSettings { SourcePath = path };
        }
    }

    public void Save() => SaveTo(SourcePath ?? SettingsPath);

    public void SaveTo(string path)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception)
        {
            // Settings are a convenience; a read-only location is not fatal.
        }
    }

    public ChunkerOptions ToChunkerOptions() => new()
    {
        MaxChunkLength = MaxChunkLength,
        ScaleSilenceToRate = ScaleSilenceToRate,
        SpeechRate = Speed,
        Silence = new SilenceOptions
        {
            LineEndingMs = SilenceLineEndingMs,
            SentenceMs = SilenceSentenceMs,
            QuestionMs = SilenceQuestionMs,
            ExclamationMs = SilenceExclamationMs,
        },
        Filters = new TextFilterOptions
        {
            FilterHashes = FilterHashes,
            FilterWebLinks = FilterWebLinks,
            FilterMailToLinks = FilterMailToLinks,
        },
    };
}
