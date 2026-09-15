/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Text.Json;

namespace TtsUtil.Core.Tts;

/// <summary>Where a speaker's name came from, which the user interface shows.</summary>
public enum SpeakerNameSource
{
    /// <summary>No name was found, so the number stands alone.</summary>
    Number,

    /// <summary>The model's own metadata, usually a piper speaker_id_map.</summary>
    Model,

    /// <summary>A speakers.txt the user wrote beside the model.</summary>
    User,
}

/// <summary>One selectable speaker inside a multi speaker voice.</summary>
public sealed class SpeakerInfo
{
    public SpeakerInfo(int id, string? name = null, SpeakerNameSource source = SpeakerNameSource.Number)
    {
        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Source = Name is null ? SpeakerNameSource.Number : source;
    }

    public int Id { get; }

    /// <summary>The name, or null when only a number is known.</summary>
    public string? Name { get; }

    public SpeakerNameSource Source { get; }

    /// <summary>What the picker shows: the number always, the name when there is one.</summary>
    public string Label => Name is null ? Id.ToString() : $"{Id}  {Name}";

    public override string ToString() => Label;
}

/// <summary>Reads speaker names for a voice, falling back to plain numbers.</summary>
public static class SpeakerCatalog
{
    /// <summary>A user written name file, one speaker per line, sitting beside the model.</summary>
    public const string SpeakerFileName = "speakers.txt";

    /// <summary>Reads names for a voice from disk, then fills the gaps with numbers.</summary>
    public static IReadOnlyList<SpeakerInfo> Load(VoiceDescriptor voice, int speakerCount)
    {
        if (speakerCount <= 0) return Array.Empty<SpeakerInfo>();

        var fromModel = ReadModelNames(voice, speakerCount);
        var fromUser = ReadUserNames(voice, speakerCount);

        return Build(speakerCount, fromModel, fromUser);
    }

    /// <summary>Combines the two name sources with numbers, the user file winning.</summary>
    public static IReadOnlyList<SpeakerInfo> Build(
        int speakerCount,
        IReadOnlyDictionary<int, string>? modelNames = null,
        IReadOnlyDictionary<int, string>? userNames = null)
    {
        var speakers = new List<SpeakerInfo>(Math.Max(0, speakerCount));

        for (var id = 0; id < speakerCount; id++)
        {
            if (userNames is not null && userNames.TryGetValue(id, out var user))
            {
                speakers.Add(new SpeakerInfo(id, user, SpeakerNameSource.User));
            }
            else if (modelNames is not null && modelNames.TryGetValue(id, out var model))
            {
                speakers.Add(new SpeakerInfo(id, model, SpeakerNameSource.Model));
            }
            else
            {
                speakers.Add(new SpeakerInfo(id));
            }
        }

        return speakers;
    }

    /// <summary>Reads the piper speaker_id_map, which maps a name to its index.</summary>
    public static IReadOnlyDictionary<int, string> ParseModelNames(string json, int speakerCount)
    {
        var names = new Dictionary<int, string>();

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("speaker_id_map", out var map)) return names;
            if (map.ValueKind != JsonValueKind.Object) return names;

            foreach (var entry in map.EnumerateObject())
            {
                if (entry.Value.ValueKind != JsonValueKind.Number) continue;
                if (!entry.Value.TryGetInt32(out var id)) continue;
                if (id < 0 || id >= speakerCount) continue;
                if (string.IsNullOrWhiteSpace(entry.Name)) continue;

                // A duplicate index would be a broken file, so the first one wins.
                names.TryAdd(id, entry.Name.Trim());
            }
        }
        catch (JsonException)
        {
            // A model shipping unreadable metadata just falls back to numbers.
        }

        return names;
    }

    /// <summary>Reads a speakers.txt of "id = name" or bare name lines, numbering bare lines
    /// from zero and skipping blanks and comments.</summary>
    public static IReadOnlyDictionary<int, string> ParseUserNames(IEnumerable<string> lines, int speakerCount)
    {
        var names = new Dictionary<int, string>();
        var nextBareId = 0;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;

            var separator = line.IndexOf('=');
            int id;
            string name;

            if (separator > 0 && int.TryParse(line[..separator].Trim(), out var explicitId))
            {
                id = explicitId;
                name = line[(separator + 1)..].Trim();
            }
            else
            {
                id = nextBareId++;
                name = line;
            }

            if (id < 0 || id >= speakerCount || name.Length == 0) continue;

            names[id] = name;
        }

        return names;
    }

    /// <summary>Orders speakers for the picker: favourites first, then a name or number match.</summary>
    public static IReadOnlyList<SpeakerInfo> Filter(
        IReadOnlyList<SpeakerInfo> speakers,
        string? query,
        IReadOnlyCollection<int>? favourites = null)
    {
        var favouriteIds = favourites is null || favourites.Count == 0
            ? null
            : new HashSet<int>(favourites);

        var matched = speakers.Where(speaker => Matches(speaker, query)).ToList();
        if (favouriteIds is null) return matched;

        // A stable partition, so inside each half the speakers keep their id order.
        var ordered = new List<SpeakerInfo>(matched.Count);
        ordered.AddRange(matched.Where(speaker => favouriteIds.Contains(speaker.Id)));
        ordered.AddRange(matched.Where(speaker => !favouriteIds.Contains(speaker.Id)));

        return ordered;
    }

    private static bool Matches(SpeakerInfo speaker, string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;

        var needle = query.Trim();

        if (speaker.Name is not null &&
            speaker.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // A number query matches the id by prefix, so 12 finds 12 and 120 but not 312.
        return speaker.Id.ToString().StartsWith(needle, StringComparison.Ordinal);
    }

    private static IReadOnlyDictionary<int, string> ReadModelNames(VoiceDescriptor voice, int speakerCount)
    {
        var metadata = voice.ModelPath + ".json";

        try
        {
            if (!File.Exists(metadata)) return new Dictionary<int, string>();
            return ParseModelNames(File.ReadAllText(metadata), speakerCount);
        }
        catch (IOException)
        {
            return new Dictionary<int, string>();
        }
        catch (UnauthorizedAccessException)
        {
            return new Dictionary<int, string>();
        }
    }

    private static IReadOnlyDictionary<int, string> ReadUserNames(VoiceDescriptor voice, int speakerCount)
    {
        var path = Path.Combine(voice.Directory, SpeakerFileName);

        try
        {
            if (!File.Exists(path)) return new Dictionary<int, string>();
            return ParseUserNames(File.ReadAllLines(path), speakerCount);
        }
        catch (IOException)
        {
            return new Dictionary<int, string>();
        }
        catch (UnauthorizedAccessException)
        {
            return new Dictionary<int, string>();
        }
    }
}
