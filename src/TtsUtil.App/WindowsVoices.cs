/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using TtsUtil.Core.Tts;
using Windows.Media.SpeechSynthesis;

namespace TtsUtil.App;

/// <summary>The speech voices Windows ships with, listed through the WinRT synthesizer, which
/// sees the ones added in Settings as well as the older SAPI set.</summary>
public static class WindowsVoices
{
    /// <summary>Lists what is installed, or nothing at all when Windows will not say.</summary>
    public static IReadOnlyList<VoiceDescriptor> Scan()
    {
        try
        {
            return SpeechSynthesizer.AllVoices
                .Where(voice => voice.Id.Length > 0)
                .Select(Describe)
                .OrderBy(voice => voice.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception)
        {
            return Array.Empty<VoiceDescriptor>();
        }
    }

    /// <summary>Finds the installed voice behind a descriptor, or null when it has gone.</summary>
    internal static VoiceInformation? Find(string id)
    {
        try
        {
            return SpeechSynthesizer.AllVoices.FirstOrDefault(voice => voice.Id == id);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static VoiceDescriptor Describe(VoiceInformation voice) => new()
    {
        Name = voice.DisplayName,
        Source = VoiceSource.Windows,
        Id = voice.Id,
        Language = voice.Language,
    };
}
