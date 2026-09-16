/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.IO;
using System.Media;
using System.Windows.Controls;
using TtsUtil.Core.Settings;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace TtsUtil.App;

/// <summary>What happens once a file has been written, which the reader picks on the Settings tab.</summary>
public partial class MainWindow
{
    private static readonly WriteFinishedNotice[] WriteNotices =
    {
        WriteFinishedNotice.None, WriteFinishedNotice.Sound, WriteFinishedNotice.PopupAndSound,
    };

    private NotifyIcon? _notifyIcon;

    /// <summary>Held down through a batch, which announces itself once at the end.</summary>
    private bool _holdWriteNotice;

    /// <summary>Stands in for the sound and the notification while testing.</summary>
    internal Action<WriteFinishedNotice, string>? WriteFinishedNotifier { get; set; }

    /// <summary>Announces a finished file, as far as the setting asks for.</summary>
    internal void NotifyWriteFinished(string path)
    {
        var notice = _settings.WriteFinishedNotice;
        if (_holdWriteNotice || notice == WriteFinishedNotice.None) return;

        (WriteFinishedNotifier ?? AnnounceWriteFinished)(notice, path);
    }

    /// <summary>Drops the tray icon the notifications are shown from.</summary>
    internal void CloseNotifications()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }

    /// <summary>Keeps a batch to one notification rather than one for every script.</summary>
    internal void HoldWriteNotices(bool held) => _holdWriteNotice = held;

    private void LoadWriteNoticeChoices()
    {
        if (SettingsWriteNoticeBox.Items.Count == 0)
        {
            SettingsWriteNoticeBox.Items.Add("No notification");
            SettingsWriteNoticeBox.Items.Add("A sound");
            SettingsWriteNoticeBox.Items.Add("A notification and a sound");
        }

        SettingsWriteNoticeBox.SelectedIndex =
            Math.Max(0, Array.IndexOf(WriteNotices, _settings.WriteFinishedNotice));
    }

    private void OnSettingsWriteNoticeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialising || SettingsWriteNoticeBox.SelectedIndex < 0) return;

        _settings.WriteFinishedNotice = WriteNotices[SettingsWriteNoticeBox.SelectedIndex];
        _settings.Save();
    }

    private void AnnounceWriteFinished(WriteFinishedNotice notice, string path)
    {
        if (notice != WriteFinishedNotice.PopupAndSound)
        {
            SystemSounds.Asterisk.Play();
            return;
        }

        // Windows plays its own notification sound over the popup.
        _notifyIcon ??= new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "TTS Util Win",
            Visible = true,
        };

        _notifyIcon.BalloonTipTitle = "The audio is ready";
        _notifyIcon.BalloonTipText = $"Wrote {Path.GetFileName(path)}.";
        _notifyIcon.ShowBalloonTip(5000);
    }
}
