/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Text;

/// <summary>Counts the characters a run will report, which is not the byte length of a file.</summary>
public static class TextMeasure
{
    private const int BufferSize = 16384;

    /// <summary>Streams the reader to its end and returns the character count.</summary>
    public static long CountCharacters(TextReader reader)
    {
        var buffer = new char[BufferSize];
        var total = 0L;

        while (true)
        {
            var read = reader.Read(buffer, 0, buffer.Length);
            if (read <= 0) break;
            total += read;
        }

        return total;
    }

    /// <summary>Opens a fresh reader and counts it, so the caller keeps its own reader unread.</summary>
    public static long CountCharacters(Func<TextReader> readerFactory)
    {
        using var reader = readerFactory();
        return CountCharacters(reader);
    }
}
