/*
 * TTS Util Win
 *
 * Derived from TTS Util (Copyright (C) 2019 Dane Finlay), Apache License 2.0.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

namespace TtsUtil.Core.Audio;

/// <summary>
/// Describes the PCM format of a wave stream.
/// </summary>
/// <remarks>
/// The Android original had to cope with whatever format the system engine
/// handed it, so it carried a full RIFF chunk model.  Here the format is always
/// produced by us, so a small record is enough.
/// </remarks>
public readonly record struct WaveFormatInfo(int SampleRate, short Channels, short BitsPerSample)
{
    public static WaveFormatInfo Pcm16Mono(int sampleRate) => new(sampleRate, 1, 16);

    public int BlockAlign => Channels * (BitsPerSample / 8);

    public int ByteRate => SampleRate * BlockAlign;

    /// <summary>Number of data bytes needed to hold the given duration.</summary>
    public int BytesForDuration(double seconds)
    {
        var bytes = (int)(ByteRate * seconds);
        return bytes - (bytes % BlockAlign);
    }
}
