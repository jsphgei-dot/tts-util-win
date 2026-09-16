using System.Diagnostics;
using Xunit;

namespace TtsUtil.App.Tests;

public sealed class AudioPlayerSinkTests
{
    /// <summary>A Windows voice hands over a whole utterance in one call, which is longer than
    /// the buffer holds. It has to be fed in as the device drains, not thrown back.</summary>
    [Fact]
    public void AnUtteranceLongerThanTheBufferIsFedInAsItPlays()
    {
        const int sampleRate = 8000;
        using var sink = new AudioPlayerSink(sampleRate, CancellationToken.None, TimeSpan.FromMilliseconds(500));

        var clock = Stopwatch.StartNew();
        sink.WriteSamples(new float[sampleRate * 2]);
        clock.Stop();

        Assert.True(clock.ElapsedMilliseconds >= 500,
            $"two seconds of audio went into half a second of buffer in {clock.ElapsedMilliseconds} ms");
    }
}
