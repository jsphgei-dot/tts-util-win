/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TtsUtil.Core.Text;

/// <summary>One "when you see this, say that" rule.</summary>
public sealed class AliasRule
{
    /// <summary>The text to look for.</summary>
    public string Match { get; set; } = string.Empty;

    /// <summary>What the voice should say instead.</summary>
    public string SayAs { get; set; } = string.Empty;

    /// <summary>Match only whole words, so dr does not fire inside dry.</summary>
    public bool WholeWord { get; set; } = true;

    public bool MatchCase { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>The id of the list that ships this rule, or empty for a rule of your own.</summary>
    public string Source { get; set; } = string.Empty;

    public AliasRule Copy() => new()
    {
        Match = Match,
        SayAs = SayAs,
        WholeWord = WholeWord,
        MatchCase = MatchCase,
        Enabled = Enabled,
        Source = Source,
    };
}

/// <summary>A list of aliases applied to text on its way to the voice. What is saved on disk
/// keeps the words that were typed.</summary>
public sealed class AliasDictionary
{
    /// <summary>Bumped only when an older file would be read wrongly.</summary>
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    public int Version { get; set; } = CurrentVersion;

    /// <summary>Shown when the list is imported, so a shared file says what it is for.</summary>
    public string Name { get; set; } = string.Empty;

    public List<AliasRule> Rules { get; set; } = new();

    /// <summary>Runs every enabled rule in order, leaving text an earlier rule wrote
    /// untouched by the rules after it.</summary>
    public string Apply(string text)
    {
        if (text.Length == 0 || Rules.Count == 0) return text;

        var pieces = new List<Piece> { new(text, Written: false) };

        foreach (var rule in Rules)
        {
            if (!rule.Enabled || rule.Match.Length == 0) continue;

            var pattern = PatternFor(rule);
            var next = new List<Piece>(pieces.Count);

            foreach (var piece in pieces)
            {
                if (piece.Written) next.Add(piece);
                else Split(piece.Text, pattern, rule.SayAs, next);
            }

            pieces = next;
        }

        var built = new StringBuilder(text.Length);
        foreach (var piece in pieces) built.Append(piece.Text);

        return built.ToString();
    }

    /// <summary>The rules that would fire on this text, for the preview box.</summary>
    public int CountMatches(string text)
    {
        var total = 0;

        foreach (var rule in Rules)
        {
            if (!rule.Enabled || rule.Match.Length == 0) continue;

            total += PatternFor(rule).Matches(text).Count;
        }

        return total;
    }

    public string ToJson()
    {
        Version = CurrentVersion;
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    /// <summary>Reads a shared file, or null when it is not one of ours.</summary>
    public static AliasDictionary? FromJson(string json)
    {
        try
        {
            var read = JsonSerializer.Deserialize<AliasDictionary>(json, JsonOptions);
            if (read is null || read.Version > CurrentVersion) return null;

            read.Rules.RemoveAll(rule => rule is null || string.IsNullOrEmpty(rule.Match));

            // A hand written file can leave a field out, and null would throw further in.
            foreach (var rule in read.Rules)
            {
                rule.SayAs ??= string.Empty;
                rule.Source ??= string.Empty;
            }

            return read;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Adds rules from another list, skipping matches this one already has.</summary>
    public int Merge(AliasDictionary other)
    {
        var known = new HashSet<string>(Rules.Select(rule => rule.Match), StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var rule in other.Rules)
        {
            if (!known.Add(rule.Match)) continue;

            Rules.Add(rule.Copy());
            added++;
        }

        return added;
    }

    private static Regex PatternFor(AliasRule rule)
    {
        var body = Regex.Escape(rule.Match);

        if (rule.WholeWord)
        {
            // A boundary either side only works next to a word character, so & and % keep none.
            if (IsWordCharacter(rule.Match[0])) body = @"\b" + body;
            if (IsWordCharacter(rule.Match[^1])) body += @"\b";
        }

        var options = RegexOptions.CultureInvariant;
        if (!rule.MatchCase) options |= RegexOptions.IgnoreCase;

        return new Regex(body, options);
    }

    private static bool IsWordCharacter(char value) => char.IsLetterOrDigit(value) || value == '_';

    private static void Split(string text, Regex pattern, string replacement, List<Piece> into)
    {
        var at = 0;

        foreach (Match match in pattern.Matches(text))
        {
            if (match.Index > at) into.Add(new Piece(text[at..match.Index], Written: false));

            into.Add(new Piece(replacement, Written: true));
            at = match.Index + match.Length;
        }

        if (at == 0) into.Add(new Piece(text, Written: false));
        else if (at < text.Length) into.Add(new Piece(text[at..], Written: false));
    }

    private readonly record struct Piece(string Text, bool Written);
}
