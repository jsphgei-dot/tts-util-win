/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.IO;
using System.Windows;
using TtsUtil.Core.Tts;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace TtsUtil.App;

/// <summary>The menu a right click opens on a voice in the Voices tab.</summary>
public partial class MainWindow
{
    /// <summary>The row the press landed on becomes the selected one, so the menu and the buttons
    /// below it act on the same voice.</summary>
    private void OnVoiceRowRightClick(object sender, MouseButtonEventArgs e)
    {
        if (RowUnder(e.OriginalSource as DependencyObject) is ListViewItem row) row.IsSelected = true;
    }

    private static ListViewItem? RowUnder(DependencyObject? source)
    {
        for (var node = source; node is not null; node = ParentOf(node))
        {
            if (node is ListViewItem row) return row;
        }

        return null;
    }

    private void OnVoiceMenuOpened(object sender, RoutedEventArgs e) => UpdateVoiceMenu();

    /// <summary>Greys the entries that would do nothing for the voice the menu was opened on.</summary>
    internal void UpdateVoiceMenu()
    {
        var row = SelectedCatalogueRow;
        var installed = row is not null && VoiceInstaller.IsInstalled(row.Voice, VoiceInstallDirectory);

        VoiceMenuInstall.IsEnabled = row is not null && !installed && !row.AddedByHand && !_installing;
        VoiceMenuRemove.IsEnabled = installed && !_installing;
        VoiceMenuWeb.IsEnabled = row?.WebAddress is not null;
        VoiceMenuFolder.IsEnabled = installed;
    }

    private void OnOpenVoiceWebLocation(object sender, RoutedEventArgs e)
    {
        if (SelectedCatalogueRow?.WebAddress is string address) OpenFolder(address);
    }

    private void OnOpenVoiceFolder(object sender, RoutedEventArgs e)
    {
        if (SelectedCatalogueRow is not VoiceCatalogueRow row) return;

        var folder = Path.Combine(VoiceInstallDirectory, row.Id);

        if (!Directory.Exists(folder))
        {
            VoiceInstallStatus.Text = $"{row.Id} is not installed.";
            return;
        }

        OpenFolder(folder);
    }
}
