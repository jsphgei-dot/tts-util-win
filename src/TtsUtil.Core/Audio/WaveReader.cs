/*
 * TTS Util Win
 *
 * Derived from TTS Util (Copyright (C) 2019 Dane Finlay), Apache License 2.0.
 */

using System.Text;

namespace TtsUtil.Core.Audio;

public sealed class IncompatibleWaveFileException : Exception
{
    public IncompatibleWaveFileException(string message) : base(message) { }
}

/// <summary>Reads the header of a RIFF/WAVE stream.</summary>
public sealed class WaveHeader
{
    private WaveHeader(WaveFormatInfo format, short audioFormat, long dataOffset, int dataSize)
    {
        Format = format;
        AudioFormat = audioFormat;
        DataOffset = dataOffset;
        DataSize = dataSize;
    }

    public WaveFormatInfo Format { get; }

    /// <summary>1 for PCM, 3 for IEEE float.</summary>
    public short AudioFormat { get; }

    /// <summary>Stream offset of the first audio data byte.</summary>
    public long DataOffset { get; }

    public int DataSize { get; }

    public TimeSpan Duration =>
        TimeSpan.FromSeconds(DataSize / (double)Format.ByteRate);

    public static WaveHeader Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        var riffId = ReadFourCc(reader);
        if (riffId != "RIFF") throw new IncompatibleWaveFileException($"Input is \"{riffId}\", not RIFF format.");
        reader.ReadInt32();

        var format = ReadFourCc(reader);
        if (format != "WAVE") throw new IncompatibleWaveFileException($"Input is \"{format}\", not WAVE format.");

        WaveFormatInfo? fmt = null;
        short audioFormat = 0;

        // Walk the sub-chunks, skipping any that are not "fmt " or "data".
        while (true)
        {
            string chunkId;
            int chunkSize;
            try
            {
                chunkId = ReadFourCc(reader);
                chunkSize = reader.ReadInt32();
            }
            catch (EndOfStreamException)
            {
                throw new IncompatibleWaveFileException("Reached end of stream before the \"data\" sub-chunk.");
            }

            if (chunkId == "fmt ")
            {
                if (chunkSize < 16) throw new IncompatibleWaveFileException($"\"fmt \" sub-chunk is {chunkSize} bytes.");
                audioFormat = reader.ReadInt16();
                var channels = reader.ReadInt16();
                var sampleRate = reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt16();
                var bitsPerSample = reader.ReadInt16();
                Skip(reader, chunkSize - 16);
                fmt = new WaveFormatInfo(sampleRate, channels, bitsPerSample);
            }
            else if (chunkId == "data")
            {
                if (fmt is null) throw new IncompatibleWaveFileException("\"data\" sub-chunk precedes \"fmt \".");
                return new WaveHeader(fmt.Value, audioFormat, stream.Position, chunkSize);
            }
            else
            {
                // Sub-chunks are word aligned.
                Skip(reader, chunkSize + (chunkSize % 2));
            }
        }
    }

    private static string ReadFourCc(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        if (bytes.Length < 4) throw new EndOfStreamException();
        return Encoding.ASCII.GetString(bytes);
    }

    private static void Skip(BinaryReader reader, int count)
    {
        if (count <= 0) return;
        if (reader.BaseStream.CanSeek) reader.BaseStream.Seek(count, SeekOrigin.Current);
        else reader.ReadBytes(count);
    }
}
