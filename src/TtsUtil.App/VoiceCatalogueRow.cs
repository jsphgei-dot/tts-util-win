/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.ComponentModel;
using TtsUtil.Core.Tts;

namespace TtsUtil.App;

/// <summary>One row of the Voices tab.</summary>
public sealed class VoiceCatalogueRow : INotifyPropertyChanged, ITickable
{
    private string _status = string.Empty;

    private bool _ticked;

    public VoiceCatalogueRow(DownloadableVoice voice)
    {
        Voice = voice;
    }

    public DownloadableVoice Voice { get; }

    public string Id => Voice.Id;

    public string Language => Voice.Language;

    public string Size => Voice.SizeMb > 0 ? $"{Voice.SizeMb} MB" : string.Empty;

    /// <summary>True for a voice found in the folder that the built in list does not know about.</summary>
    public bool AddedByHand { get; init; }

    /// <summary>Where the voice came from, or null when nothing on the web is known for it.</summary>
    public string? WebAddress => AddedByHand ? null : DownloadableVoices.PageUrl;

    public string Licence => Voice.Licence;

    /// <summary>Whether Install should take this voice in its batch.</summary>
    public bool Ticked
    {
        get => _ticked;
        set
        {
            if (_ticked == value) return;

            _ticked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Ticked)));
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Names the row for accessibility tools, which otherwise read the class name.</summary>
    public override string ToString() => $"{Id}, {Language}, {Size}, {Status}";
}
