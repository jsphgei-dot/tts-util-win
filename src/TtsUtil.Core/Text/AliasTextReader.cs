/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Text;

namespace TtsUtil.Core.Text;

/// <summary>Applies aliases a line at a time on the way past, so a replacement is never split
/// across two chunks.</summary>
public sealed class AliasTextReader : TextReader
{
    private readonly TextReader _inner;
    private readonly AliasDictionary _aliases;
    private readonly StringBuilder _ready = new();
    private int _at;
    private bool _drained;

    public AliasTextReader(TextReader inner, AliasDictionary aliases)
    {
        _inner = inner;
        _aliases = aliases;
    }

    /// <summary>Wraps the reader only when there is a rule that could fire.</summary>
    public static TextReader Wrap(TextReader inner, AliasDictionary? aliases) =>
        aliases is null || aliases.Rules.Count == 0 ? inner : new AliasTextReader(inner, aliases);

    public override int Peek() => Fill() ? _ready[_at] : -1;

    public override int Read() => Fill() ? _ready[_at++] : -1;

    public override int Read(char[] buffer, int index, int count)
    {
        var written = 0;

        while (written < count && Fill())
        {
            var take = Math.Min(count - written, _ready.Length - _at);
            _ready.CopyTo(_at, buffer, index + written, take);
            _at += take;
            written += take;
        }

        return written;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>Pulls another line through the rules when the last one has been handed out.</summary>
    private bool Fill()
    {
        while (_at >= _ready.Length)
        {
            if (_drained) return false;

            _ready.Clear();
            _at = 0;

            var line = ReadLineKeepingEnding();
            if (line is null)
            {
                _drained = true;
                return false;
            }

            _ready.Append(line);
        }

        return true;
    }

    private string? ReadLineKeepingEnding()
    {
        var line = new StringBuilder();

        while (true)
        {
            var next = _inner.Read();

            if (next < 0) break;

            if (next == '\n')
            {
                // The ending goes on after the rules, which never see it and cannot eat it.
                return _aliases.Apply(line.ToString()) + '\n';
            }

            line.Append((char)next);
        }

        return line.Length == 0 ? null : _aliases.Apply(line.ToString());
    }
}
