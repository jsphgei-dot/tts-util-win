/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.IO;
using NAudio.MediaFoundation;
using NAudio.Wave;
using TtsUtil.Core.Tts;

namespace TtsUtil.App;

/// <summary>Writes an MP3 by buffering PCM to a temporary wave file and encoding it once.</summary>
public sealed class Mp3FileSink : ISampleSink, IDisposable
{
    private readonly WaveFileSink _pcm;
    private readonly string _temporaryPath;
    private readonly int _bitRate;
    private readonly Action<string>? _progress;
    private bool _encoded;

    public Mp3FileSink(string path, int sampleRate, int bitRate = 128000, Action<string>? progress = null)
    {
        Path = path;
        _bitRate = bitRate;
        _progress = progress;
        _temporaryPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(path) ?? System.IO.Path.GetTempPath(),
            System.IO.Path.GetFileNameWithoutExtension(path) + ".ttsutil.tmp.wav");

        _pcm = new WaveFileSink(_temporaryPath, sampleRate);
    }

    public string Path { get; }

    public TimeSpan Duration => _pcm.Duration;

    /// <summary>True when Windows can encode MP3, which every supported version can.</summary>
    public static bool IsAvailable
    {
        get
        {
            try
            {
                MediaFoundationApi.Startup();
                return MediaFoundationEncoder.SelectMediaType(
                    AudioSubtypes.MFAudioFormat_MP3, new WaveFormat(22050, 16, 1), 128000) is not null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public void WriteSamples(ReadOnlySpan<float> samples) => _pcm.WriteSamples(samples);

    public void WriteSilence(int milliseconds) => _pcm.WriteSilence(milliseconds);

    /// <summary>Encodes and removes the temporary wave file. Runs on the calling thread.</summary>
    public void Dispose()
    {
        if (_encoded)
        {
            return;
        }

        _encoded = true;
        _pcm.Dispose();

        try
        {
            _progress?.Invoke("Encoding MP3...");
            MediaFoundationApi.Startup();

            using (var reader = new AudioFileReader(_temporaryPath))
            {
                MediaFoundationEncoder.EncodeToMp3(reader, Path, _bitRate);
            }
        }
        finally
        {
            TryDeleteTemporary();
        }
    }

    private void TryDeleteTemporary()
    {
        try
        {
            if (File.Exists(_temporaryPath)) File.Delete(_temporaryPath);
        }
        catch (IOException)
        {
            // A leftover temporary file is untidy, not a failure worth reporting.
        }
    }
}
