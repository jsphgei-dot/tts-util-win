/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TtsUtil.Core.Text;

/// <summary>How the case buttons rewrite a block.</summary>
public enum LetterCase
{
    Upper,
    Lower,
    Sentence,
    Title,
}

/// <summary>Toolbar editing, as functions over a block of whole lines of plain text.</summary>
public static class TextTools
{
    /// <summary>The marker the bullet button puts in front of a line.</summary>
    public const string Bullet = "• ";

    /// <summary>One step of indentation, written as spaces.</summary>
    public const string Indentation = "    ";

    private static readonly Regex BulletPrefix = new(@"^[ \t]*[•●*\-]\s+", RegexOptions.Compiled);

    private static readonly Regex NumberPrefix = new(@"^[ \t]*\d+[.)]\s+", RegexOptions.Compiled);

    private static readonly Regex RepeatedSpaces = new(@"[ \t]{2,}", RegexOptions.Compiled);

    /// <summary>Puts a bullet in front of every line, or takes them all off when all have one.</summary>
    public static string ToggleBullets(string block) =>
        AllMarked(block, BulletPrefix)
            ? MapLines(block, line => BulletPrefix.Replace(line, string.Empty))
            : MapLines(block, line => line.Length == 0 ? line : Bullet + BulletPrefix.Replace(line, string.Empty));

    /// <summary>Numbers the lines from one, or takes the numbers off when every line has one.</summary>
    public static string ToggleNumbers(string block)
    {
        if (AllMarked(block, NumberPrefix))
        {
            return MapLines(block, line => NumberPrefix.Replace(line, string.Empty));
        }

        var n = 0;
        return MapLines(block, line =>
        {
            if (line.Length == 0) return line;

            n++;
            return $"{n}. " + NumberPrefix.Replace(line, string.Empty);
        });
    }

    /// <summary>Adds one step of indentation to every line.</summary>
    public static string Indent(string block) =>
        MapLines(block, line => line.Length == 0 ? line : Indentation + line);

    /// <summary>Takes one step off, accepting a tab or fewer spaces than a full step.</summary>
    public static string Outdent(string block) => MapLines(block, line =>
    {
        if (line.StartsWith("\t", StringComparison.Ordinal)) return line.Substring(1);

        var spaces = 0;
        while (spaces < Indentation.Length && spaces < line.Length && line[spaces] == ' ') spaces++;

        return line.Substring(spaces);
    });

    /// <summary>Rewrites the case of a block. Sentence and title case leave acronyms alone.</summary>
    public static string ChangeCase(string block, LetterCase letterCase) => letterCase switch
    {
        LetterCase.Upper => block.ToUpper(CultureInfo.CurrentCulture),
        LetterCase.Lower => block.ToLower(CultureInfo.CurrentCulture),
        LetterCase.Title => ToTitleCase(block),
        _ => ToSentenceCase(block),
    };

    /// <summary>Turns a run of blank lines into a single blank line.</summary>
    public static string CollapseBlankLines(string block)
    {
        var lines = Split(block, out var newLine);
        var kept = new List<string>(lines.Length);
        var blankRun = 0;

        foreach (var line in lines)
        {
            if (line.Trim().Length == 0)
            {
                blankRun++;
                if (blankRun > 1) continue;
            }
            else
            {
                blankRun = 0;
            }

            kept.Add(line);
        }

        return string.Join(newLine, kept);
    }

    /// <summary>Joins wrapped lines into one. Leaves a line that ends a sentence, opens a list
    /// or starts indented.</summary>
    public static string JoinWrappedLines(string block)
    {
        var lines = Split(block, out var newLine);
        var joined = new List<string>(lines.Length);

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();

            if (joined.Count == 0 || line.Length == 0 || joined[^1].Length == 0 || StartsNewBlock(line)
                || EndsParagraph(joined[^1]))
            {
                joined.Add(line);
                continue;
            }

            joined[^1] = joined[^1] + " " + line.TrimStart();
        }

        return string.Join(newLine, joined);
    }

    /// <summary>Trims trailing blanks and squeezes runs of spaces.</summary>
    public static string TidySpacing(string block) =>
        MapLines(block, line => RepeatedSpaces.Replace(line.TrimEnd(), " "));

    /// <summary>Replaces every occurrence and reports how many there were.</summary>
    public static (string Text, int Count) ReplaceAll(string text, string find, string replace,
        bool caseSensitive)
    {
        if (string.IsNullOrEmpty(find)) return (text, 0);

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.CurrentCultureIgnoreCase;
        var builder = new StringBuilder(text.Length);
        var count = 0;
        var at = 0;

        while (true)
        {
            var found = text.IndexOf(find, at, comparison);
            if (found < 0) break;

            builder.Append(text, at, found - at).Append(replace);
            at = found + find.Length;
            count++;
        }

        if (count == 0) return (text, 0);

        builder.Append(text, at, text.Length - at);
        return (builder.ToString(), count);
    }

    private static bool AllMarked(string block, Regex prefix)
    {
        var lines = Split(block, out _).Where(l => l.Trim().Length > 0).ToList();
        return lines.Count > 0 && lines.All(l => prefix.IsMatch(l));
    }

    private static bool StartsNewBlock(string line) =>
        BulletPrefix.IsMatch(line) || NumberPrefix.IsMatch(line)
        || line.StartsWith(" ", StringComparison.Ordinal)
        || line.StartsWith("\t", StringComparison.Ordinal);

    private static bool EndsParagraph(string line)
    {
        var last = line.TrimEnd();
        if (last.Length == 0) return true;

        return ".!?:;\"”’".IndexOf(last[^1]) >= 0;
    }

    private static string ToTitleCase(string block) => MapWords(block, (word, _) =>
        IsAcronym(word)
            ? word
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(word.ToLower(CultureInfo.CurrentCulture)));

    private static string ToSentenceCase(string block) => MapWords(block, (word, first) =>
    {
        if (IsAcronym(word)) return word;

        var lower = word.ToLower(CultureInfo.CurrentCulture);
        return first ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lower) : lower;
    });

    /// <summary>True for a word already written entirely in capitals.</summary>
    private static bool IsAcronym(string word) =>
        word.Length > 1 && word.ToUpper(CultureInfo.CurrentCulture) == word;

    /// <summary>Walks the block word by word, telling the callback when a word opens a sentence.</summary>
    private static string MapWords(string block, Func<string, bool, string> map)
    {
        var builder = new StringBuilder(block.Length);
        var word = new StringBuilder();
        var sentenceOpen = true;

        void Flush()
        {
            if (word.Length == 0) return;

            builder.Append(map(word.ToString(), sentenceOpen));
            sentenceOpen = false;
            word.Clear();
        }

        foreach (var c in block)
        {
            if (char.IsLetterOrDigit(c) || c == '\'' || c == '’')
            {
                word.Append(c);
                continue;
            }

            Flush();
            builder.Append(c);

            if (c is '.' or '!' or '?' or '\n') sentenceOpen = true;
        }

        Flush();
        return builder.ToString();
    }

    private static string MapLines(string block, Func<string, string> map)
    {
        var lines = Split(block, out var newLine);
        return string.Join(newLine, lines.Select(map));
    }

    /// <summary>Splits on either line ending and reports which one to put back.</summary>
    private static string[] Split(string block, out string newLine)
    {
        newLine = block.Contains('\r') ? "\r\n" : "\n";
        return block.Replace("\r\n", "\n").Split('\n');
    }
}
