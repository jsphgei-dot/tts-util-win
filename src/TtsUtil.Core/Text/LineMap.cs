/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Text;

/// <summary>One line of the script and where it sits in the text.</summary>
public readonly record struct LineSpan(int Number, int Start, int Length, string Text)
{
    /// <summary>One past the last character of the line, excluding its line break.</summary>
    public int End => Start + Length;
}

/// <summary>Maps between character offsets and line numbers, so a run can start anywhere.</summary>
public sealed class LineMap
{
    private readonly IReadOnlyList<LineSpan> _lines;

    private LineMap(IReadOnlyList<LineSpan> lines) => _lines = lines;

    public int Count => _lines.Count;

    public LineSpan this[int index] => _lines[Math.Clamp(index, 0, _lines.Count - 1)];

    public IReadOnlyList<LineSpan> Lines => _lines;

    /// <summary>Splits on CRLF, LF and CR alike, keeping blank lines and their offsets.</summary>
    public static LineMap Build(string? text)
    {
        var lines = new List<LineSpan>();
        text ??= string.Empty;

        var start = 0;
        var index = 0;

        while (index < text.Length)
        {
            var c = text[index];
            if (c != '\n' && c != '\r')
            {
                index++;
                continue;
            }

            lines.Add(new LineSpan(lines.Count, start, index - start, text[start..index]));

            // A CRLF pair is one break, not two.
            if (c == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;

            index++;
            start = index;
        }

        // Text that does not end in a break still has a last line; text that does, does not.
        if (start < text.Length || lines.Count == 0)
        {
            lines.Add(new LineSpan(lines.Count, start, text.Length - start, text[start..]));
        }

        return new LineMap(lines);
    }

    /// <summary>The line holding the given offset, clamped to the ends of the text.</summary>
    public int LineAt(long characterOffset)
    {
        if (characterOffset <= 0) return 0;

        for (var i = _lines.Count - 1; i >= 0; i--)
        {
            if (characterOffset >= _lines[i].Start) return i;
        }

        return 0;
    }

    /// <summary>Where a line starts, for a run that should begin part way through.</summary>
    public int StartOf(int lineIndex) => this[lineIndex].Start;

    /// <summary>Lines that would produce no speech, so the picker can skip past them.</summary>
    public bool IsBlank(int lineIndex) => this[lineIndex].Text.Trim().Length == 0;
}
