/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.ComponentModel;
using TtsUtil.Core.Settings;

namespace TtsUtil.App;

/// <summary>A saved script in the list, with the tick that puts it in a batch conversion.</summary>
public sealed class ScriptRow : INotifyPropertyChanged, ITickable
{
    private bool _chosen;

    public ScriptRow(SavedScript script, string? voice = null)
    {
        Script = script;
        Voice = string.IsNullOrWhiteSpace(voice) ? NoVoice : voice;
    }

    /// <summary>What the voice column shows for a script saved before voices were kept.</summary>
    public const string NoVoice = "Not saved";

    public SavedScript Script { get; }

    public string Title => Script.Title;

    public string Label => Script.ToString();

    /// <summary>The voice this script was last read with.</summary>
    public string Voice { get; }

    public bool Chosen
    {
        get => _chosen;
        set
        {
            if (_chosen == value) return;

            _chosen = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chosen)));
        }
    }

    bool ITickable.Ticked
    {
        get => Chosen;
        set => Chosen = value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => Label;
}
