/*
 * TTS Util Win
 *
 * Derived from TTS Util (Copyright (C) 2022 Dane Finlay), Apache License 2.0.
 */

using System.Text;

namespace TtsUtil.Core.Text;

/// <summary>Which text elements are omitted from synthesis.</summary>
public sealed class TextFilterOptions
{
    public bool FilterHashes { get; set; }

    public bool FilterWebLinks { get; set; }

    public bool FilterMailToLinks { get; set; }

    public bool AnyEnabled => FilterHashes || FilterWebLinks || FilterMailToLinks;

    public TextFilterOptions Clone() => new()
    {
        FilterHashes = FilterHashes,
        FilterWebLinks = FilterWebLinks,
        FilterMailToLinks = FilterMailToLinks,
    };
}

/// <summary>Removes filtered characters and words from a character buffer.</summary>
public static class TextFilters
{
    private const int Hash = 0x23;

    /// <summary>Removes filtered content in place, returning the number of characters removed.</summary>
    public static int Apply(List<char> buffer, TextFilterOptions options)
    {
        if (!options.AnyEnabled || buffer.Count == 0) return 0;

        var initialSize = buffer.Count;
        var word = new StringBuilder();
        var marked = new SortedSet<int>();

        for (var index = 0; index < buffer.Count; index++)
        {
            var c = buffer[index];
            var notWhitespace = !char.IsWhiteSpace(c);
            if (notWhitespace)
            {
                word.Append(c);
                if (FilterChar(c, options)) marked.Add(index);
                if (index < buffer.Count - 1) continue;
            }

            // A word boundary was reached.
            var text = word.ToString();
            if (FilterWord(text, options))
            {
                var wordIndexZero = index - text.Length;
                if (notWhitespace) wordIndexZero++;
                for (var i = 0; i < text.Length; i++) marked.Add(wordIndexZero + i);
            }

            word.Clear();
        }

        foreach (var index in marked.Reverse()) buffer.RemoveAt(index);
        return initialSize - buffer.Count;
    }

    public static bool FilterChar(char c, TextFilterOptions options) =>
        options.FilterHashes && c == Hash;

    public static bool FilterWord(string word, TextFilterOptions options)
    {
        if (word.Length == 0) return false;

        var lower = word.ToLowerInvariant();

        if (options.FilterWebLinks &&
            (lower.StartsWith("http://", StringComparison.Ordinal) ||
             lower.StartsWith("https://", StringComparison.Ordinal)) &&
            Uri.TryCreate(lower, UriKind.Absolute, out var url) &&
            !string.IsNullOrEmpty(url.Host))
        {
            return true;
        }

        if (options.FilterMailToLinks &&
            lower.StartsWith("mailto:", StringComparison.Ordinal) &&
            Uri.TryCreate(lower, UriKind.Absolute, out var mail) &&
            mail.Scheme == "mailto" &&
            !string.IsNullOrEmpty(mail.Host))
        {
            return true;
        }

        return false;
    }
}
