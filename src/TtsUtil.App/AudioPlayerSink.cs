/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using NAudio.Wave;
using TtsUtil.Core.Audio;
using TtsUtil.Core.Tts;

namespace TtsUtil.App;

/// <summary>Plays synthesised audio through the default output device.</summary>
public sealed class AudioPlayerSink : IAudioPlayback
{
    private static readonly TimeSpan MaxQueued = TimeSpan.FromSeconds(4);

    private readonly WaveOutEvent _output;
    private readonly BufferedWaveProvider _buffer;
    private readonly CancellationToken _cancellationToken;
    private readonly PlaybackMarks _marks = new();
    private long _bytesQueued;
    private byte[] _scratch = Array.Empty<byte>();
    private bool _disposed;
    private volatile bool _paused;

    public AudioPlayerSink(int sampleRate, CancellationToken cancellationToken, TimeSpan? capacity = null)
    {
        _cancellationToken = cancellationToken;

        var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        _buffer = new BufferedWaveProvider(format)
        {
            BufferDuration = capacity ?? TimeSpan.FromSeconds(30),
            DiscardOnBufferOverflow = false,
            ReadFully = true,
        };

        _output = new WaveOutEvent { DesiredLatency = 200 };
        _output.Init(_buffer);
        _output.Play();
    }

    public bool IsPaused => _paused;

    /// <summary>Where the listener has reached, which trails what has been synthesised.</summary>
    public long PlayedCharacters => _marks.CharactersAt(PlayedBytes());

    public void Mark(long characterOffset) => _marks.Add(_bytesQueued, characterOffset);

    /// <summary>Holds the device. Synthesis carries on until its few seconds of lookahead fill.</summary>
    public void Pause()
    {
        if (_paused) return;

        _paused = true;
        try
        {
            _output.Pause();
        }
        catch (Exception)
        {
            // The device may already be gone.
        }
    }

    public void Resume()
    {
        if (!_paused) return;

        _paused = false;
        try
        {
            _output.Play();
        }
        catch (Exception)
        {
            // The device may already be gone.
        }
    }

    public void WriteSamples(ReadOnlySpan<float> samples)
    {
        if (samples.Length == 0) return;

        var needed = samples.Length * 4;
        if (_scratch.Length < needed) _scratch = new byte[Math.Max(needed, 16384)];

        var offset = 0;
        foreach (var sample in samples)
        {
            var bits = BitConverter.SingleToInt32Bits(sample);
            _scratch[offset++] = (byte)bits;
            _scratch[offset++] = (byte)(bits >> 8);
            _scratch[offset++] = (byte)(bits >> 16);
            _scratch[offset++] = (byte)(bits >> 24);
        }

        Write(_scratch, needed);
    }

    public void WriteSilence(int milliseconds)
    {
        if (milliseconds <= 0) return;

        var format = _buffer.WaveFormat;
        var remaining = (int)(format.AverageBytesPerSecond * (milliseconds / 1000.0));
        remaining -= remaining % format.BlockAlign;

        var block = new byte[Math.Min(Math.Max(remaining, format.BlockAlign), 16384)];
        while (remaining > 0 && !_cancellationToken.IsCancellationRequested)
        {
            var count = Math.Min(block.Length, remaining);
            count -= count % format.BlockAlign;
            if (count == 0) break;

            Write(block, count);
            remaining -= count;
        }
    }

    /// <summary>Blocks until queued audio has finished playing or the task is cancelled.</summary>
    public void WaitUntilDrained()
    {
        // A paused device is not draining, but the audio is still owed to the listener.
        while (!_cancellationToken.IsCancellationRequested &&
               (_paused || _output.PlaybackState == PlaybackState.Playing) &&
               _buffer.BufferedBytes > 0)
        {
            Thread.Sleep(50);
        }

        // Let the device play out what it has already been handed.
        if (!_cancellationToken.IsCancellationRequested) Thread.Sleep(_output.DesiredLatency);
    }

    public void Stop()
    {
        _paused = false;
        _marks.Clear();

        try
        {
            _buffer.ClearBuffer();
            _output.Stop();
        }
        catch (Exception)
        {
            // The device may already be gone.
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _output.Dispose();
    }

    /// <summary>Bytes the listener has actually heard, allowing for what sits in the device.</summary>
    private long PlayedBytes()
    {
        var drained = _bytesQueued - _buffer.BufferedBytes;
        var inDevice = _buffer.WaveFormat.AverageBytesPerSecond * (long)_output.DesiredLatency / 1000;
        return Math.Max(0, drained - inDevice);
    }

    /// <summary>Hands audio over in pieces the buffer has room for, since a Windows voice
    /// arrives as one whole utterance and the buffer throws rather than waiting.</summary>
    private void Write(byte[] data, int count)
    {
        var align = _buffer.WaveFormat.BlockAlign;
        var offset = 0;

        while (offset < count)
        {
            WaitForRoom();
            if (_cancellationToken.IsCancellationRequested) return;

            var take = Math.Min(count - offset, _buffer.BufferLength - _buffer.BufferedBytes);
            take -= take % align;
            if (take == 0) continue;

            _buffer.AddSamples(data, offset, take);
            _bytesQueued += take;
            offset += take;
        }
    }

    private void WaitForRoom()
    {
        while (!_cancellationToken.IsCancellationRequested && _buffer.BufferedDuration > MaxQueued)
        {
            Thread.Sleep(20);
        }
    }
}
