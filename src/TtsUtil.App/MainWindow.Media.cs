/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Interop;
using Windows.Media;

namespace TtsUtil.App;

/// <summary>The media keys and the Windows media overlay, driving the same playback the
/// buttons drive.</summary>
public partial class MainWindow
{
    /// <summary>The transport controls, or null when Windows would not give them out.</summary>
    internal MediaControls? Media { get; private set; }

    /// <summary>Attaches the transport controls once the window has a handle to ask with.</summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        Media = MediaControls.ForWindow(new WindowInteropHelper(this).Handle);
        if (Media is null) return;

        Media.ButtonPressed += button => Dispatcher.BeginInvoke(() => OnMediaButton(button));
    }

    /// <summary>Runs the pressed media key against the reading that is in flight.</summary>
    internal void OnMediaButton(SystemMediaTransportControlsButton button)
    {
        switch (button)
        {
            case SystemMediaTransportControlsButton.Play:
                if (ActivePlayback?.IsPaused == true) OnTogglePause(this, new RoutedEventArgs());
                else if (!_busy) OnReadText(this, new RoutedEventArgs());
                break;

            case SystemMediaTransportControlsButton.Pause:
                if (ActivePlayback is { IsPaused: false }) OnTogglePause(this, new RoutedEventArgs());
                break;

            case SystemMediaTransportControlsButton.Stop:
                StopRun("Stopping...");
                break;
        }
    }

    private void ShowMediaState(MediaState state) => Media?.Show(state);
}
