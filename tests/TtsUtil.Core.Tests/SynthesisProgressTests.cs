using System.Text;
using TtsUtil.Core.Text;
using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class SynthesisProgressTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ttsutil-progress-" + Guid.NewGuid().ToString("N"));

    public SynthesisProgressTests() => Directory.CreateDirectory(_root);

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
    public void ProgressEndsAtTheCharacterCountNotTheByteCount()
    {
        const string text = "Le café était naïve. Über straße! 你好世界?";
        var path = Path.Combine(_root, "accented.txt");
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var bytes = new FileInfo(path).Length;
        var total = TextMeasure.CountCharacters(() => new StreamReader(path, detectEncodingFromByteOrderMarks: true));

        var reports = Run(() => new StreamReader(path, detectEncodingFromByteOrderMarks: true), total);
        var last = reports[^1];

        Assert.True(bytes > total, $"the fixture must be multi byte: {bytes} bytes, {total} characters");
        Assert.Equal(100, last.Percent);
        Assert.Equal(total, last.TotalCharacters);
        Assert.Equal(total, last.CharactersRead);
    }

    [Fact]
    public void AByteLengthTotalWouldStallTheBarBelowWhatWasRead()
    {
        const string text = "你好世界你好世界你好世界。";
        var path = Path.Combine(_root, "cjk.txt");
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var bytes = new FileInfo(path).Length;
        var characters = TextMeasure.CountCharacters(() => new StreamReader(path, detectEncodingFromByteOrderMarks: true));

        var withBytes = Run(() => new StreamReader(path, detectEncodingFromByteOrderMarks: true), bytes);
        var beforeTheForcedFinish = withBytes[^2];

        // Three bytes per character here, so a byte total leaves the bar around a third.
        Assert.True(beforeTheForcedFinish.Percent < 50,
            $"a byte total should understate progress, saw {beforeTheForcedFinish.Percent}%");

        var withCharacters = Run(() => new StreamReader(path, detectEncodingFromByteOrderMarks: true), characters);

        // With the right total the bar is at the cap of 99 before the run forces 100.
        Assert.True(withCharacters[^2].Percent >= 90,
            $"a character total should reach the end, saw {withCharacters[^2].Percent}%");
        Assert.Equal(characters, withCharacters[^1].CharactersRead);
    }

    [Fact]
    public void PlainAsciiIsUnaffected()
    {
        const string text = "One sentence. Another sentence.";
        var path = Path.Combine(_root, "ascii.txt");
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var characters = TextMeasure.CountCharacters(() => new StreamReader(path));

        Assert.Equal(new FileInfo(path).Length, characters);

        var reports = Run(() => new StreamReader(path), characters);
        Assert.Equal(100, reports[^1].Percent);
    }

    private static List<SynthesisProgress> Run(Func<TextReader> readerFactory, long total)
    {
        var reports = new List<SynthesisProgress>();
        var runner = new SynthesisRunner(new SilentEngine(), new ChunkerOptions());

        using var reader = readerFactory();
        runner.Run(reader, total, new NullSink(), new SynchronousProgress(reports.Add));

        return reports;
    }

    private sealed class SynchronousProgress : IProgress<SynthesisProgress>
    {
        private readonly Action<SynthesisProgress> _report;

        public SynchronousProgress(Action<SynthesisProgress> report) => _report = report;

        public void Report(SynthesisProgress value) => _report(value);
    }

    private sealed class SilentEngine : ITtsEngine
    {
        public VoiceDescriptor Voice { get; } = new() { Name = "fake" };

        public int SampleRate => 22050;

        public int SpeakerCount => 1;

        public void Synthesize(string text, int speakerId, float speed, SampleCallback onSamples,
            CancellationToken cancellationToken)
        {
            onSamples(new float[16]);
        }

        public void Dispose()
        {
        }
    }

    private sealed class NullSink : ISampleSink
    {
        public void WriteSamples(ReadOnlySpan<float> samples)
        {
        }

        public void WriteSilence(int milliseconds)
        {
        }
    }
}
