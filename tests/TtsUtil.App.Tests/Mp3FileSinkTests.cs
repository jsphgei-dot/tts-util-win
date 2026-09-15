using System.IO;
using Xunit;

namespace TtsUtil.App.Tests;

public sealed class Mp3FileSinkTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ttsutil-mp3-" + Guid.NewGuid().ToString("N"));

    public Mp3FileSinkTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void DisposingEncodesAnMp3AndClearsUpAfterItself()
    {
        var path = Path.Combine(_root, "spoken.mp3");
        var tone = new float[22050];
        for (var i = 0; i < tone.Length; i++) tone[i] = (float)Math.Sin(i * 0.05) * 0.4f;

        var reported = new List<string>();
        using (var sink = new Mp3FileSink(path, 22050, 128000, reported.Add))
        {
            sink.WriteSamples(tone);
            sink.WriteSilence(200);

            Assert.False(File.Exists(path), "nothing is encoded until the sink is disposed");
        }

        var bytes = File.ReadAllBytes(path);

        Assert.True(bytes.Length > 1000, $"expected a real MP3, got {bytes.Length} bytes");
        Assert.True(IsMp3(bytes), "the file does not start with an ID3 tag or a frame sync");
        Assert.Empty(Directory.GetFiles(_root, "*.tmp.wav"));
        Assert.Contains("Encoding MP3...", reported);
    }

    [Fact]
    public void DisposingTwiceDoesNotEncodeTwiceOrThrow()
    {
        var path = Path.Combine(_root, "once.mp3");
        var sink = new Mp3FileSink(path, 22050);
        sink.WriteSamples(new float[22050]);

        sink.Dispose();
        var first = new FileInfo(path).LastWriteTimeUtc;
        sink.Dispose();

        Assert.Equal(first, new FileInfo(path).LastWriteTimeUtc);
    }

    private static bool IsMp3(IReadOnlyList<byte> bytes) =>
        (bytes[0] == 'I' && bytes[1] == 'D' && bytes[2] == '3') ||
        (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0);
}
