/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Audio;

/// <summary>Maps a position in the audio queued so far back to the character being spoken.</summary>
public sealed class PlaybackMarks
{
    private readonly Queue<(long Bytes, long Characters)> _pending = new();
    private readonly object _gate = new();
    private long _reached;

    /// <summary>Records that audio queued from this byte on speaks text from this character.</summary>
    public void Add(long bytesQueued, long characterOffset)
    {
        lock (_gate) _pending.Enqueue((bytesQueued, characterOffset));
    }

    /// <summary>The character being spoken once this many bytes have left the queue.</summary>
    public long CharactersAt(long playedBytes)
    {
        lock (_gate)
        {
            while (_pending.Count > 0 && _pending.Peek().Bytes <= playedBytes)
            {
                _reached = _pending.Dequeue().Characters;
            }

            return _reached;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _pending.Clear();
            _reached = 0;
        }
    }
}
