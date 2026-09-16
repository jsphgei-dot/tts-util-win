/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.ComponentModel;
using TtsUtil.Core.Settings;

namespace TtsUtil.App;

/// <summary>A saved script in the list, with the tick that puts it in a batch conversion.</summary>
public sealed class ScriptRow : INotifyPropertyChanged
{
    private bool _chosen;

    public ScriptRow(SavedScript script) => Script = script;

    public SavedScript Script { get; }

    public string Title => Script.Title;

    public string Label => Script.ToString();

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => Label;
}
