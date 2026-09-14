/*
 * TTS Util Win
 *
 * Derived from TTS Util (Copyright (C) 2019 Dane Finlay), Apache License 2.0.
 */

using System.Text;

namespace TtsUtil.Core.Audio;

/// <summary>Streaming writer for 16 bit PCM RIFF/WAVE files.</summary>
public sealed class WaveWriter : IDisposable
{
    private const int HeaderSize = 44;

    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly BinaryWriter _writer;
    private readonly long _headerStart;
    private byte[] _scratch = Array.Empty<byte>();
    private bool _disposed;

    public WaveWriter(Stream stream, WaveFormatInfo format, bool leaveOpen = false)
    {
        if (!stream.CanWrite) throw new ArgumentException("Stream is not writable.", nameof(stream));
        if (!stream.CanSeek) throw new ArgumentException("Stream is not seekable.", nameof(stream));
        if (format.BitsPerSample != 16) throw new ArgumentException("Only 16 bit PCM is supported.", nameof(format));

        _stream = stream;
        _leaveOpen = leaveOpen;
        Format = format;
        _headerStart = stream.Position;
        _writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        WriteHeader(0);
    }

    public WaveFormatInfo Format { get; }

    /// <summary>Number of audio data bytes written so far.</summary>
    public int DataBytesWritten { get; private set; }

    /// <summary>Duration of the audio written so far.</summary>
    public TimeSpan Duration =>
        TimeSpan.FromSeconds(DataBytesWritten / (double)Format.ByteRate);

    /// <summary>Appends samples in the range [-1, 1], clamping on overflow.</summary>
    public void WriteSamples(ReadOnlySpan<float> samples)
    {
        if (samples.Length == 0) return;

        var needed = samples.Length * 2;
        if (_scratch.Length < needed) _scratch = new byte[Math.Max(needed, 8192)];

        var offset = 0;
        foreach (var sample in samples)
        {
            var scaled = (int)MathF.Round(Math.Clamp(sample, -1f, 1f) * short.MaxValue);
            var value = (short)Math.Clamp(scaled, short.MinValue, short.MaxValue);
            _scratch[offset++] = (byte)(value & 0xff);
            _scratch[offset++] = (byte)((value >> 8) & 0xff);
        }

        _stream.Write(_scratch, 0, needed);
        DataBytesWritten += needed;
    }

    /// <summary>Appends the given number of milliseconds of silence.</summary>
    public void WriteSilence(int milliseconds)
    {
        if (milliseconds <= 0) return;

        var remaining = Format.BytesForDuration(milliseconds / 1000.0);
        if (remaining == 0) return;

        var block = new byte[Math.Min(remaining, 16384)];
        while (remaining > 0)
        {
            var count = Math.Min(block.Length, remaining);
            _stream.Write(block, 0, count);
            DataBytesWritten += count;
            remaining -= count;
        }
    }

    public void Flush() => _stream.Flush();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Patch the placeholder sizes in the header.
        var end = _stream.Position;
        _stream.Position = _headerStart;
        WriteHeader(DataBytesWritten);
        _stream.Position = end;
        _stream.Flush();

        _writer.Dispose();
        if (!_leaveOpen) _stream.Dispose();
    }

    private void WriteHeader(int dataSize)
    {
        _writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        _writer.Write(HeaderSize - 8 + dataSize);
        _writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        _writer.Write(Encoding.ASCII.GetBytes("fmt "));
        _writer.Write(16);
        _writer.Write((short)1);
        _writer.Write(Format.Channels);
        _writer.Write(Format.SampleRate);
        _writer.Write(Format.ByteRate);
        _writer.Write((short)Format.BlockAlign);
        _writer.Write(Format.BitsPerSample);

        _writer.Write(Encoding.ASCII.GetBytes("data"));
        _writer.Write(dataSize);
        _writer.Flush();
    }
}
