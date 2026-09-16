/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows.Controls;
using Control = System.Windows.Controls.Control;

namespace TtsUtil.App;

/// <summary>The settings a run in flight has already taken a copy of, held still while it plays.</summary>
public partial class MainWindow
{
    internal const string LockedWhilePlaying = "Stop the playback before changing this.";

    private readonly Dictionary<Control, object?> _toolTipsWhenFree = new();

    /// <summary>Changing any of these mid run either does nothing or pulls the voice away.</summary>
    private IEnumerable<Control> PlaybackSettings()
    {
        yield return VoiceBox;
        yield return SpeakerBox;
        yield return SpeedSlider;
        yield return RescanButton;
        yield return UseWindowsVoicesBox;
        yield return InstallVoiceButton;
        yield return RemoveVoiceButton;
    }

    /// <summary>Greys the settings out and says why on hover, which needs the tooltip turned on
    /// for disabled controls.</summary>
    internal void LockPlaybackSettings(bool locked)
    {
        foreach (var control in PlaybackSettings())
        {
            if (!_toolTipsWhenFree.ContainsKey(control)) _toolTipsWhenFree[control] = control.ToolTip;

            ToolTipService.SetShowOnDisabled(control, true);
            ToolTipService.SetPlacement(control, System.Windows.Controls.Primitives.PlacementMode.Mouse);

            // An install running on the Voices tab keeps its own two buttons down either way.
            var held = _installing && (control == InstallVoiceButton || control == RemoveVoiceButton);

            control.IsEnabled = !locked && !held;
            control.ToolTip = locked ? LockedWhilePlaying : _toolTipsWhenFree[control];
        }
    }
}
