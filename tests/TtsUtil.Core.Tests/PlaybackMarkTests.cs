using TtsUtil.Core.Audio;
using TtsUtil.Core.Text;
using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class PlaybackMarkTests
{
    [Fact]
    public void EachUtteranceIsMarkedWithItsOwnStartNotTheNextOne()
    {
        var sink = new RecordingSink();
        var runner = new SynthesisRunner(new SilentEngine(), new ChunkerOptions());

        runner.Run(new StringReader("One.\nTwo.\nThree."), 16, sink);

        // A mark must land before that utterance's audio, and point at where it begins.
        Assert.Equal(new[] { "mark 0", "audio", "mark 5", "audio", "mark 10", "audio" }, sink.Events);
    }

    [Fact]
    public void APositionBetweenMarksReportsTheOneAlreadyBeingSpoken()
    {
        var marks = new PlaybackMarks();
        marks.Add(0, 0);
        marks.Add(1000, 40);
        marks.Add(2000, 90);

        Assert.Equal(0, marks.CharactersAt(999));
        Assert.Equal(40, marks.CharactersAt(1500));
        Assert.Equal(90, marks.CharactersAt(5000));

        // Playback does not run backwards, so neither does the line it reports.
        Assert.Equal(90, marks.CharactersAt(0));
    }

    private sealed class RecordingSink : ISampleSink
    {
        public List<string> Events { get; } = new();

        public void WriteSamples(ReadOnlySpan<float> samples) => Events.Add("audio");

        public void WriteSilence(int milliseconds)
        {
        }

        public void Mark(long characterOffset) => Events.Add($"mark {characterOffset}");
    }

    private sealed class SilentEngine : ITtsEngine
    {
        public VoiceDescriptor Voice { get; } = new() { Name = "fake" };

        public int SampleRate => 22050;

        public int SpeakerCount => 1;

        public void Synthesize(string text, int speakerId, float speed, SampleCallback onSamples,
            CancellationToken cancellationToken) => onSamples(new float[16]);

        public void Dispose()
        {
        }
    }
}
