/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.IO;
using System.Windows;
using TtsUtil.Core.Tts;

namespace TtsUtil.App;

/// <summary>Voices taken in from a link of the reader's own, alongside the built in list.</summary>
public partial class MainWindow
{
    /// <summary>The import started from the tab, kept so tests can wait for it.</summary>
    internal Task VoiceImportRun { get; private set; } = Task.CompletedTask;

    private void OnVoiceSources(object sender, RoutedEventArgs e) =>
        new VoiceSourcesWindow(OpenFolder) { Owner = this }.ShowDialog();

    private void OnImportVoiceLibrary(object sender, RoutedEventArgs e) =>
        VoiceImportRun = ImportVoiceFromLinkAsync(VoiceLinkBox.Text);

    /// <summary>Downloads and unpacks the archive a link points at, reporting as it goes.</summary>
    internal async Task ImportVoiceFromLinkAsync(string? url)
    {
        if (_installing)
        {
            VoiceInstallStatus.Text = "Already installing. Press Cancel first.";
            return;
        }

        if (VoiceArchiveLink.NameFrom(url) is not string name)
        {
            VoiceInstallStatus.Text = "Paste an https link to a voice archive, such as one ending .tar.bz2.";
            return;
        }

        _installing = true;
        _installCancellation = new CancellationTokenSource();
        InstallVoiceButton.IsEnabled = false;
        RemoveVoiceButton.IsEnabled = false;
        ImportVoiceButton.IsEnabled = false;
        CancelVoiceButton.IsEnabled = true;
        VoiceProgress.Value = 0;

        var progress = new Progress<VoiceInstallProgress>(report =>
        {
            if (!_installing) return;

            if (report.Phase == VoiceInstallPhase.Downloading)
            {
                VoiceProgress.Value = report.Percent;
                VoiceInstallStatus.Text = $"Downloading {name}: {report.Percent}%";
            }
            else if (report.Phase == VoiceInstallPhase.Extracting)
            {
                VoiceProgress.Value = 100;
                VoiceInstallStatus.Text = $"Unpacking {name}...";
            }
        });

        try
        {
            var folder = await VoiceInstallerFactory()
                .InstallFromLinkAsync(url!.Trim(), VoiceInstallDirectory, progress, _installCancellation.Token);

            VoiceProgress.Value = 100;
            VoiceInstallStatus.Text =
                $"{Path.GetFileName(folder)} imported. Read the license in its folder before you use it.";
            VoiceLinkBox.Clear();
            DisposeEngine();
            RefreshVoices();
        }
        catch (OperationCanceledException)
        {
            VoiceProgress.Value = 0;
            VoiceInstallStatus.Text = $"{name} canceled; nothing was kept.";
        }
        catch (Exception ex)
        {
            VoiceProgress.Value = 0;
            VoiceInstallStatus.Text = $"{name} could not be imported: {ex.Message}";
        }
        finally
        {
            _installCancellation?.Dispose();
            _installCancellation = null;
            _installing = false;
            InstallVoiceButton.IsEnabled = true;
            RemoveVoiceButton.IsEnabled = true;
            ImportVoiceButton.IsEnabled = true;
            CancelVoiceButton.IsEnabled = false;
            RefreshVoiceCatalogue();
        }
    }
}
