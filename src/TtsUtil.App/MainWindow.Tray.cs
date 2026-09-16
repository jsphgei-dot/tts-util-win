/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;

namespace TtsUtil.App;

/// <summary>Closing the window can put it among the hidden icons in the notification area rather
/// than end the program, so a long reading or a file being written carries on.</summary>
public partial class MainWindow
{
    private TrayPresence? _tray;
    private bool _quitting;

    /// <summary>Whether the window is in the notification area rather than on screen.</summary>
    internal bool InTray { get; private set; }

    private void HideToTray()
    {
        _tray ??= new TrayPresence("TTS Util Win", RestoreFromTray, QuitFromTray);
        _tray.Show("Still reading. Open it again from this icon, or quit from its menu.");
        InTray = true;
        Hide();
    }

    /// <summary>Brings the window back and takes the icon away again.</summary>
    internal void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        _tray?.Hide();
        InTray = false;
    }

    /// <summary>Ends the program from the icon's menu, rather than hiding again.</summary>
    internal void QuitFromTray()
    {
        _quitting = true;
        Close();
    }
}
