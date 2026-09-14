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
    public string ResolvedVoicesDirectory =>
        string.IsNullOrWhiteSpace(VoicesDirectory)
            ? FindDefaultVoicesDirectory()
            : VoicesDirectory!;

    [JsonIgnore]
    public string ResolvedOutputDirectory =>
        string.IsNullOrWhiteSpace(OutputDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            : OutputDirectory!;

    /// <summary>Directory holding the running executable.</summary>
    public static string AppDirectory =>
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    public static string SettingsPath
    {
        get
        {
            var portable = Path.Combine(AppDirectory, "settings.json");
            if (File.Exists(portable) || IsWritable(AppDirectory)) return portable;

            var roaming = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TtsUtilWin");
            Directory.CreateDirectory(roaming);
            return Path.Combine(roaming, "settings.json");
        }
    }

    public static AppSettings Load()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new AppSettings();
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
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

    /// <summary>Prefers voices beside the exe, then the nearest voices folder up the tree.</summary>
    private static string FindDefaultVoicesDirectory()
    {
        var beside = Path.Combine(AppDirectory, "voices");
        if (HasVoices(beside)) return beside;

        var current = new DirectoryInfo(AppDirectory);
        for (var depth = 0; depth < 6 && current?.Parent is not null; depth++)
        {
            current = current.Parent;
            var candidate = Path.Combine(current.FullName, "voices");
            if (HasVoices(candidate)) return candidate;
        }

        return beside;
    }

    private static bool HasVoices(string directory)
    {
        try
        {
            return Directory.Exists(directory) && Directory.EnumerateDirectories(directory).Any();
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool IsWritable(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, $".write-probe-{Guid.NewGuid():N}");
            using (File.Create(probe)) { }
            File.Delete(probe);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
