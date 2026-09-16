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

    public string Size => $"{Voice.SizeMb} MB";

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
