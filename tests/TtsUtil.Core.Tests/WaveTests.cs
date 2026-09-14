using TtsUtil.Core.Audio;
using Xunit;

namespace TtsUtil.Core.Tests;

public class WaveTests
{
    [Fact]
    public void WrittenHeaderReadsBackWithTheSameFormat()
    {
        using var stream = new MemoryStream();
        var format = WaveFormatInfo.Pcm16Mono(22050);

        using (var writer = new WaveWriter(stream, format, leaveOpen: true))
        {
            writer.WriteSamples(new float[] { 0f, 0.5f, -0.5f, 1f });
        }

        stream.Position = 0;
        var header = WaveHeader.Read(stream);

        Assert.Equal(22050, header.Format.SampleRate);
        Assert.Equal(1, header.Format.Channels);
        Assert.Equal(16, header.Format.BitsPerSample);
        Assert.Equal(1, header.AudioFormat);
        Assert.Equal(8, header.DataSize);
        Assert.Equal(44, header.DataOffset);
    }

    [Fact]
    public void SamplesAreScaledAndClamped()
    {
        using var stream = new MemoryStream();

        using (var writer = new WaveWriter(stream, WaveFormatInfo.Pcm16Mono(16000), leaveOpen: true))
        {
            writer.WriteSamples(new[] { 0f, 1f, -1f, 2f, -2f });
        }

        var bytes = stream.ToArray();
        var samples = new short[5];
        for (var i = 0; i < samples.Length; i++) samples[i] = BitConverter.ToInt16(bytes, 44 + i * 2);

        Assert.Equal(0, samples[0]);
        Assert.Equal(short.MaxValue, samples[1]);
        Assert.Equal(-short.MaxValue, samples[2]);
        Assert.Equal(short.MaxValue, samples[3]);
        Assert.Equal(-short.MaxValue, samples[4]);
    }

    [Fact]
    public void SilenceHasTheExpectedLength()
    {
        using var stream = new MemoryStream();
        var format = WaveFormatInfo.Pcm16Mono(16000);

        using (var writer = new WaveWriter(stream, format, leaveOpen: true))
        {
            writer.WriteSilence(500);
            Assert.Equal(format.BytesForDuration(0.5), writer.DataBytesWritten);
            Assert.Equal(500, writer.Duration.TotalMilliseconds, 1);
        }

        stream.Position = 0;
        var header = WaveHeader.Read(stream);

        Assert.Equal(16000, header.DataSize);
        Assert.All(stream.ToArray()[44..], b => Assert.Equal(0, b));
    }

    [Fact]
    public void NonRiffInputIsRejected()
    {
        using var stream = new MemoryStream(new byte[64]);

        Assert.Throws<IncompatibleWaveFileException>(() => WaveHeader.Read(stream));
    }

    [Fact]
    public void UnknownChunksAreSkipped()
    {
        using var stream = new MemoryStream();
        WriteHeaderWithListChunk(stream);

        stream.Position = 0;
        var header = WaveHeader.Read(stream);

        Assert.Equal(8000, header.Format.SampleRate);
        Assert.Equal(4, header.DataSize);
    }

    private static void WriteHeaderWithListChunk(Stream stream)
    {
        var writer = new BinaryWriter(stream);
        writer.Write("RIFF".ToCharArray());
        writer.Write(4 + 24 + 12 + 12);
        writer.Write("WAVE".ToCharArray());

        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(8000);
        writer.Write(16000);
        writer.Write((short)2);
        writer.Write((short)16);

        writer.Write("LIST".ToCharArray());
        writer.Write(4);
        writer.Write("INFO".ToCharArray());

        writer.Write("data".ToCharArray());
        writer.Write(4);
        writer.Write(new byte[4]);
        writer.Flush();
    }
}
