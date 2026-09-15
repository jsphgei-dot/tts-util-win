/*
 * TTS Util Win
 *
 * Derived from TTS Util (Copyright (C) 2022 Dane Finlay), Apache License 2.0.
 */

using System.Text;

namespace TtsUtil.Core.Text;

/// <summary>One unit of text to synthesise, plus trailing silence.</summary>
public readonly record struct Utterance(string Text, int SilenceMs, long InputStartIndex, int CharsRead);

/// <summary>Silence durations inserted for each kind of delimiter.</summary>
public sealed class SilenceOptions
{
    public int LineEndingMs { get; set; } = 200;

    public int SentenceMs { get; set; }

    public int QuestionMs { get; set; }

    public int ExclamationMs { get; set; }

    public SilenceOptions Clone() => new()
    {
        LineEndingMs = LineEndingMs,
        SentenceMs = SentenceMs,
        QuestionMs = QuestionMs,
        ExclamationMs = ExclamationMs,
    };
}

public sealed class ChunkerOptions
{
    public int MaxChunkLength { get; set; } = 2000;

    public double LineFeedScanThreshold { get; set; } = 0.70;

    public double WhitespaceScanThreshold { get; set; } = 0.90;

    public SilenceOptions Silence { get; set; } = new();

    public TextFilterOptions Filters { get; set; } = new();

    public SpokenCharacterOptions Spoken { get; set; } = new();

    /// <summary>Divides silence durations by the speech rate when true.</summary>
    public bool ScaleSilenceToRate { get; set; }

    public float SpeechRate { get; set; } = 1.0f;
}

/// <summary>Splits input text into utterances at delimiters that carry silence.</summary>
public sealed class TextChunker
{
    private readonly ChunkerOptions _options;
    private readonly Dictionary<int, int> _delimiterSilence;
    private readonly HashSet<int> _endOfTextDelimiters;
    private readonly int _maxLength;
    private readonly int _lineFeedThreshold;
    private readonly int _whitespaceThreshold;

    public TextChunker(ChunkerOptions options)
    {
        _options = options;
        var silence = options.Silence;

        // Unicode halfwidth and fullwidth forms are aliased to their ASCII counterparts.
        _delimiterSilence = new Dictionary<int, int>
        {
            [0x000a] = silence.LineEndingMs,

            [0x002e] = silence.SentenceMs,
            [0x2026] = silence.SentenceMs,
            [0xff0e] = silence.SentenceMs,
            [0xff61] = silence.SentenceMs,

            [0x003f] = silence.QuestionMs,
            [0xff1f] = silence.QuestionMs,

            [0x0021] = silence.ExclamationMs,
            [0xff01] = silence.ExclamationMs,
        };

        _endOfTextDelimiters = new HashSet<int>(_delimiterSilence.Keys);
        _endOfTextDelimiters.Remove(0x000a);

        _maxLength = Math.Max(16, options.MaxChunkLength);
        _lineFeedThreshold = (int)(_maxLength * options.LineFeedScanThreshold);
        _whitespaceThreshold = (int)(_maxLength * options.WhitespaceScanThreshold);
    }

    /// <summary>Total number of characters removed by the text filters.</summary>
    public long CharactersFiltered { get; private set; }

    /// <summary>Total number of input characters consumed.</summary>
    public long CharactersRead { get; private set; }

    public IEnumerable<Utterance> Read(TextReader reader)
    {
        while (true)
        {
            var utterance = ReadNext(reader, out var more);
            if (utterance is { } value && value.CharsRead > 0 &&
                (value.Text.Length > 0 || value.SilenceMs > 0))
            {
                yield return value;
            }

            if (!more) yield break;
        }
    }

    public IEnumerable<Utterance> Read(string text) => Read(new StringReader(text));

    private Utterance? ReadNext(TextReader reader, out bool moreInput)
    {
        var buffer = new List<char>();
        var charsRead = 0;
        var silenceMs = 0;
        var silenceSource = -1;

        var next = reader.Read();
        while (next >= 0)
        {
            charsRead++;
            var c = (char)next;
            silenceMs = _delimiterSilence.TryGetValue(next, out var ms) ? ms : 0;
            if (silenceMs > 0) silenceSource = next;

            if (silenceMs == 0)
            {
                buffer.Add(c);
            }
            else
            {
                // A delimiter needs a preceding word character before it counts,
                // so runs such as "??" do not each produce silence.
                if (_endOfTextDelimiters.Contains(next))
                {
                    var last = buffer.Count > 0 ? buffer[^1] : (char?)null;
                    if (last is { } lastChar && !char.IsWhiteSpace(lastChar) &&
                        !_delimiterSilence.ContainsKey(lastChar)) break;
                }
                else
                {
                    break;
                }
            }

            if (buffer.Count >= _lineFeedThreshold && next == 0x0a) break;
            if (buffer.Count >= _whitespaceThreshold && char.IsWhiteSpace(c)) break;
            if (buffer.Count >= _maxLength) break;

            next = reader.Read();
        }

        moreInput = next >= 0;
        if (charsRead == 0) return null;

        // Runs such as "What???" pause once, while blank lines still stack up.
        if (buffer.Count == 0 && silenceMs > 0 && _endOfTextDelimiters.Contains(silenceSource)) silenceMs = 0;

        if (_options.Filters.AnyEnabled)
        {
            CharactersFiltered += TextFilters.Apply(buffer, _options.Filters);
        }

        CharactersFiltered += SpokenCharacters.Apply(buffer, _options.Spoken);

        var builder = new StringBuilder(buffer.Count);
        foreach (var c in buffer) builder.Append(c);

        var startIndex = CharactersRead;
        CharactersRead += charsRead;

        if (silenceMs > 0 && _options.ScaleSilenceToRate && _options.SpeechRate > 0)
        {
            silenceMs = (int)(silenceMs / _options.SpeechRate);
        }

        return new Utterance(builder.ToString(), silenceMs, startIndex, charsRead);
    }
}
