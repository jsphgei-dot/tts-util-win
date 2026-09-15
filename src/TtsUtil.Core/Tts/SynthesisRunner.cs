/*
 * TTS Util Win
 *
 * Derived from TTS Util (Copyright (C) 2022 Dane Finlay), Apache License 2.0.
 */

using TtsUtil.Core.Audio;
using TtsUtil.Core.Text;

namespace TtsUtil.Core.Tts;

/// <summary>Destination for synthesised audio.</summary>
public interface ISampleSink
{
    void WriteSamples(ReadOnlySpan<float> samples);

    void WriteSilence(int milliseconds);
}

/// <summary>A sink that plays as it receives, so it can be paused and stopped.</summary>
public interface IAudioPlayback : ISampleSink, IDisposable
{
    bool IsPaused { get; }

    void Pause();

    void Resume();

    void Stop();

    /// <summary>Blocks until queued audio has played out, or the run is cancelled.</summary>
    void WaitUntilDrained();
}

/// <summary>Writes synthesised audio to a wave file.</summary>
public sealed class WaveFileSink : ISampleSink, IDisposable
{
    private readonly WaveWriter _writer;

    public WaveFileSink(string path, int sampleRate)
    {
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        _writer = new WaveWriter(stream, WaveFormatInfo.Pcm16Mono(sampleRate));
        Path = path;
    }

    public string Path { get; }

    public TimeSpan Duration => _writer.Duration;

    public void WriteSamples(ReadOnlySpan<float> samples) => _writer.WriteSamples(samples);

    public void WriteSilence(int milliseconds) => _writer.WriteSilence(milliseconds);

    public void Dispose() => _writer.Dispose();
}

/// <summary>Progress of a running synthesis task.</summary>
public readonly record struct SynthesisProgress(int Percent, long CharactersRead, long TotalCharacters);

/// <summary>Drives text through the chunker and the engine into a sink.</summary>
public sealed class SynthesisRunner
{
    private readonly ITtsEngine _engine;
    private readonly ChunkerOptions _options;

    public SynthesisRunner(ITtsEngine engine, ChunkerOptions options)
    {
        _engine = engine;
        _options = options;
    }

    public int SpeakerId { get; set; }

    public float Speed { get; set; } = 1.0f;

    /// <summary>Number of characters removed by the text filters during the last run.</summary>
    public long CharactersFiltered { get; private set; }

    /// <summary>How many utterances actually reached the engine during the last run.</summary>
    public long UtterancesSpoken { get; private set; }

    public void Run(
        TextReader input,
        long totalCharacters,
        ISampleSink sink,
        IProgress<SynthesisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var chunker = new TextChunker(_options);
        var speed = Speed <= 0 ? 1.0f : Speed;
        CharactersFiltered = 0;
        UtterancesSpoken = 0;

        progress?.Report(new SynthesisProgress(0, 0, totalCharacters));

        foreach (var utterance in chunker.Read(input))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (utterance.Text.Trim().Length > 0)
            {
                UtterancesSpoken++;
                _engine.Synthesize(utterance.Text, SpeakerId, speed, samples =>
                {
                    if (cancellationToken.IsCancellationRequested) return false;
                    sink.WriteSamples(samples.Span);
                    return true;
                }, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            sink.WriteSilence(utterance.SilenceMs);

            CharactersFiltered = chunker.CharactersFiltered;
            progress?.Report(BuildProgress(chunker.CharactersRead, totalCharacters));
        }

        progress?.Report(new SynthesisProgress(100, chunker.CharactersRead, totalCharacters));
    }

    private static SynthesisProgress BuildProgress(long read, long total)
    {
        var percent = total > 0 ? (int)Math.Clamp(read * 100 / total, 0, 99) : 0;
        return new SynthesisProgress(percent, read, total);
    }
}
