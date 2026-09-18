/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Tts;

/// <summary>Receives generated audio; returning false aborts the utterance.</summary>
public delegate bool SampleCallback(ReadOnlyMemory<float> samples);

/// <summary>A loaded voice that turns text into audio samples.</summary>
public interface ITtsEngine : IDisposable
{
    VoiceDescriptor Voice { get; }

    int SampleRate { get; }

    int SpeakerCount { get; }

    /// <summary>Synthesizes one utterance, delivering samples as they are produced.</summary>
    void Synthesize(string text, int speakerId, float speed, SampleCallback onSamples, CancellationToken cancellationToken);

    /// <summary>True once the voice has spoken, so its random state no longer matches a fresh load.</summary>
    bool NeedsVoiceReset => false;

    /// <summary>Winds the voice back to the state it was loaded in, so the same text sounds the same.</summary>
    void ResetVoice()
    {
    }
}
