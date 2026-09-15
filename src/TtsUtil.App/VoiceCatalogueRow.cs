/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.ComponentModel;
using TtsUtil.Core.Tts;

namespace TtsUtil.App;

/// <summary>One row of the Voices tab.</summary>
public sealed class VoiceCatalogueRow : INotifyPropertyChanged
{
    private string _status = string.Empty;

    public VoiceCatalogueRow(DownloadableVoice voice)
    {
        Voice = voice;
    }

    public DownloadableVoice Voice { get; }

    public string Id => Voice.Id;

    public string Language => Voice.Language;

    public string Size => $"{Voice.SizeMb} MB";

    public string Licence => Voice.Licence;

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
