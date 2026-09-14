/*
 * TTS Util Win
 *
 * Derived from TTS Util (Copyright (C) 2022 Dane Finlay), Apache License 2.0.
 */

using System.Text;

namespace TtsUtil.Core.Text;

/// <summary>Decides what to speak as text is typed, for playback on input mode.</summary>
public static class TypingReader
{
    /// <summary>Returns the text to speak for one edit, or null when nothing should be spoken.</summary>
    public static string? TextToRead(string text, int changeOffset, int addedLength, int removedLength)
    {
        if (text.Length == 0 || addedLength <= 0 || addedLength < removedLength) return null;
        if (changeOffset < 0 || changeOffset + addedLength > text.Length) return null;

        var inserted = text.Substring(changeOffset, addedLength);

        // A typed delimiter completes the word before it; ordinary characters say nothing.
        if (addedLength == 1)
        {
            return IsDelimiter(inserted[0]) ? WordEndingAt(text, changeOffset) : null;
        }

        // A wholesale replacement, such as a paste, is read from start to end.
        if (changeOffset == 0 && addedLength >= removedLength) return inserted;

        return null;
    }

    /// <summary>Returns the word immediately before the delimiter, including it.</summary>
    private static string? WordEndingAt(string text, int delimiterIndex)
    {
        var builder = new StringBuilder();
        builder.Append(text[delimiterIndex]);

        for (var i = delimiterIndex - 1; i >= 0; i--)
        {
            var c = text[i];
            if (IsDelimiter(c)) break;
            builder.Append(c);
        }

        if (builder.Length <= 1) return null;

        var chars = builder.ToString().ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>Whitespace and sentence punctuation, including fullwidth forms.</summary>
    public static bool IsDelimiter(char c)
    {
        int cp = c;
        return cp == 0x0021 || cp == 0xff01
            || cp == 0x0022 || cp == 0xff02
            || cp == 0x002c || cp == 0xff0c
            || cp == 0xff64
            || cp == 0x002e || cp == 0xff0e
            || cp == 0xff61
            || cp == 0x003a || cp == 0xff1a
            || cp == 0x003b || cp == 0xff1b
            || cp == 0x003f || cp == 0xff1f
            || cp == 0x2026
            || char.IsWhiteSpace(c);
    }
}
