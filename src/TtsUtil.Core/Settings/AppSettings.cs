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
        Converters = { new JsonStringEnumConverter() },
    };

    public string? VoicesDirectory { get; set; }

    public string? LastVoiceName { get; set; }

    /// <summary>The speaker used before choices became per voice, still the fallback.</summary>
    public int SpeakerId { get; set; }

    /// <summary>The chosen speaker for each voice, keyed by lower case voice name.</summary>
    public Dictionary<string, int> SpeakerIds { get; set; } = new();

    /// <summary>Speakers the user starred, keyed by lower case voice name.</summary>
    public Dictionary<string, List<int>> FavouriteSpeakers { get; set; } = new();

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

    /// <summary>Which characters are allowed to be voiced. Punctuation still becomes silence.</summary>
    public SpokenCharacterPolicy SpokenCharacters { get; set; } = SpokenCharacterPolicy.LatinOnly;

    /// <summary>Extra characters allowed through on top of the policy.</summary>
    public string AllowedExtraCharacters { get; set; } = string.Empty;

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

    /// <summary>The speaker last used for a voice, falling back to the shared setting.</summary>
    public int GetSpeakerId(string? voiceName)
    {
        var key = VoiceKey(voiceName);
        if (key is not null && SpeakerIds.TryGetValue(key, out var id)) return id;
        return SpeakerId;
    }

    /// <summary>Records the speaker for one voice, and as the fallback for a new voice.</summary>
    public void SetSpeakerId(string? voiceName, int speakerId)
    {
        SpeakerId = speakerId;

        var key = VoiceKey(voiceName);
        if (key is null) return;
        SpeakerIds[key] = speakerId;
    }

    /// <summary>The starred speakers for a voice, in the order they were starred.</summary>
    public IReadOnlyList<int> GetFavouriteSpeakers(string? voiceName)
    {
        var key = VoiceKey(voiceName);
        if (key is null || !FavouriteSpeakers.TryGetValue(key, out var ids)) return Array.Empty<int>();
        return ids;
    }

    public bool IsFavouriteSpeaker(string? voiceName, int speakerId) =>
        GetFavouriteSpeakers(voiceName).Contains(speakerId);

    /// <summary>Stars or unstars a speaker, returning whether it is starred afterwards.</summary>
    public bool ToggleFavouriteSpeaker(string? voiceName, int speakerId)
    {
        var key = VoiceKey(voiceName);
        if (key is null) return false;

        if (!FavouriteSpeakers.TryGetValue(key, out var ids))
        {
            ids = new List<int>();
            FavouriteSpeakers[key] = ids;
        }

        if (ids.Remove(speakerId))
        {
            // An empty list would only grow the settings file with nothing in it.
            if (ids.Count == 0) FavouriteSpeakers.Remove(key);
            return false;
        }

        ids.Add(speakerId);
        return true;
    }

    private static string? VoiceKey(string? voiceName) =>
        string.IsNullOrWhiteSpace(voiceName) ? null : voiceName!.Trim().ToLowerInvariant();

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
        Spoken = new SpokenCharacterOptions
        {
            Policy = SpokenCharacters,
            AllowedExtra = AllowedExtraCharacters ?? string.Empty,
        },
    };
}
