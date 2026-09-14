/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Tts;

public enum VoiceModelKind
{
    Vits,
    Matcha,
    Kokoro,
}

/// <summary>A sherpa model directory that can be loaded as a voice.</summary>
public sealed class VoiceDescriptor
{
    public string Name { get; init; } = string.Empty;

    public string Directory { get; init; } = string.Empty;

    public VoiceModelKind Kind { get; init; }

    /// <summary>The acoustic model, or the single model for vits and kokoro.</summary>
    public string ModelPath { get; init; } = string.Empty;

    public string TokensPath { get; init; } = string.Empty;

    /// <summary>Vocoder model, used by matcha voices only.</summary>
    public string? VocoderPath { get; init; }

    /// <summary>Speaker embedding file, used by kokoro voices only.</summary>
    public string? VoicesBinPath { get; init; }

    public string? LexiconPath { get; init; }

    /// <summary>espeak-ng data directory, when the model needs one.</summary>
    public string? DataDirPath { get; init; }

    /// <summary>jieba dictionary directory, for Chinese models.</summary>
    public string? DictDirPath { get; init; }

    public string? LicensePath { get; init; }

    public string? ModelCardPath { get; init; }

    public override string ToString() => Name;
}
