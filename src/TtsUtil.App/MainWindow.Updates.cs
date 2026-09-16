/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using TtsUtil.Core;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Update;

namespace TtsUtil.App;

/// <summary>
/// The look at the published manifest, made once each time the program starts. A portable copy
/// is pointed at the release page; an installed copy can be offered the setup program.
/// </summary>
public partial class MainWindow
{
    /// <summary>Reads the manifest. Tests replace it so no request is made.</summary>
    internal Func<CancellationToken, Task<UpdateManifest?>> UpdateFetcher { get; set; } =
        async cancellationToken =>
        {
            using var checker = new UpdateChecker();
            return await checker.FetchAsync(UpdateSource.ManifestUrl, cancellationToken);
        };

    /// <summary>Fetches and unpacks the setup program. Tests replace it so nothing is downloaded.</summary>
    internal Func<UpdateDownload, IProgress<UpdateProgress>, CancellationToken,
        Task<(UpdateFetchResult Result, string? SetupPath)>> UpdateFetchSetup
    { get; set; } =
        async (download, progress, cancellationToken) =>
        {
            using var installer = new UpdateInstaller();
            var folder = Path.Combine(Path.GetTempPath(), "TtsUtilWinUpdate");
            return await installer.FetchAsync(download, folder, progress, cancellationToken);
        };

    /// <summary>Starts the setup program and leaves. Tests replace it so nothing is launched.</summary>
    internal Action<string> SetupLauncher { get; set; } = path =>
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        System.Windows.Application.Current?.Shutdown();
    };

    /// <summary>True when this copy runs from a portable folder, which tests override.</summary>
    internal Func<bool> IsPortableCopy { get; set; } = () => AppSettings.IsPortable;

    /// <summary>The check that runs at startup, kept so tests can wait for it.</summary>
    internal Task UpdateCheck { get; private set; } = Task.CompletedTask;

    /// <summary>The install started from the tab, kept so tests can wait for it.</summary>
    internal Task InstallRun { get; private set; } = Task.CompletedTask;

    /// <summary>The newer version the last check found, which the Install button acts on.</summary>
    private UpdateManifest? _newVersion;

    /// <summary>Looks for a newer release on the way in, and never in a way that blocks.</summary>
    internal void StartUpdateCheck(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (!UpdateChecker.IsDue(_settings.CheckForUpdates)) return;

        UpdateCheck = RunUpdateCheckAsync(now, asked: false);
    }

    /// <summary>The box lives on its own tab, so it saves itself rather than waiting for Apply.</summary>
    private void OnCheckForUpdatesChanged(object sender, RoutedEventArgs e)
    {
        _settings.CheckForUpdates = CheckForUpdatesBox.IsChecked == true;
        _settings.Save();
    }

    /// <summary>Update dialogs are opt in, so the tab is the only place a new version waits.</summary>
    private void OnPromptForUpdatesChanged(object sender, RoutedEventArgs e)
    {
        _settings.PromptForUpdates = PromptForUpdatesBox.IsChecked == true;
        _settings.Save();
    }

    /// <summary>Fills the Updates tab with what is known before any check has run.</summary>
    internal void ShowUpdateState()
    {
        UpdateVersionText.Text = $"You are running {AppVersion.Name}";
        UpdateStateText.Text = "Press Check now to ask the release page for a newer version.";

        ShowChangelog();

        LastUpdateCheckText.Text = _settings.LastUpdateCheckUtc is DateTime last
            ? $"Last checked {last.ToLocalTime():d MMM yyyy, HH:mm}"
            : "Not checked yet";
    }

    /// <summary>The change history shipped inside this build, listed newest first.</summary>
    internal void ShowChangelog()
    {
        ChangelogList.ItemsSource ??= Changelog.Parse(ReadChangelog());
    }

    private static string ReadChangelog()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("CHANGELOG.md");
        if (stream is null) return string.Empty;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>The mark on the tab header and the new version notes under it, which is all a new
    /// version does while the dialog is turned off.</summary>
    private void MarkNewVersion(UpdateManifest? manifest)
    {
        _newVersion = manifest;
        UpdatesTabMark.Visibility = manifest is null ? Visibility.Collapsed : Visibility.Visible;
        NewVersionPanel.Visibility = manifest is null ? Visibility.Collapsed : Visibility.Visible;
        InstallUpdateButton.IsEnabled = manifest is not null;

        UpdateStateText.Text = manifest is null
            ? $"{AppVersion.Name} is the newest version."
            : $"Version {manifest.VersionName} is available. You are running {AppVersion.Name}.";

        if (manifest is null) return;

        NewVersionTitle.Text = $"New in {manifest.VersionName}";
        NewVersionNotes.ItemsSource = manifest.Notes;
        NewVersionEmptyText.Visibility = manifest.Notes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Looks now, whatever the schedule says, and reports what it finds either way.</summary>
    private void OnCheckForUpdates(object sender, RoutedEventArgs e)
    {
        if (!CheckForUpdatesButton.IsEnabled) return;

        CheckForUpdatesButton.IsEnabled = false;
        SetStatus("Looking for a new version...");
        UpdateCheck = RunUpdateCheckAsync(DateTime.UtcNow, asked: true);
    }

    private async Task RunUpdateCheckAsync(DateTime nowUtc, bool asked)
    {
        try
        {
            await CheckAsync(nowUtc, asked);
        }
        finally
        {
            CheckForUpdatesButton.IsEnabled = true;
        }
    }

    private async Task CheckAsync(DateTime nowUtc, bool asked)
    {
        UpdateManifest? manifest;

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            manifest = await UpdateFetcher(timeout.Token);
        }
        catch (Exception)
        {
            // A check nobody asked for is a courtesy, and failing one is not worth saying.
            if (asked) SetStatus("Could not reach the release page. Try again later.");
            return;
        }

        if (manifest is null)
        {
            if (asked) SetStatus("Could not read the list of releases. Try again later.");
            return;
        }

        _settings.LastUpdateCheckUtc = nowUtc;
        _settings.Save();
        LastUpdateCheckText.Text = $"Last checked {nowUtc.ToLocalTime():d MMM yyyy, HH:mm}";

        // Asking outright outranks an earlier no to that same version.
        var dismissed = asked ? 0 : _settings.DismissedUpdateCode;
        var action = UpdateDecision.For(manifest, AppVersion.Code, IsPortableCopy(), dismissed);

        // The mark follows the version itself, so an offer turned down still shows on the tab.
        MarkNewVersion(manifest.VersionCode > AppVersion.Code ? manifest : null);

        if (action == UpdateAction.None)
        {
            if (asked) SetStatus($"You are running {AppVersion.Name}, which is the newest version.");

            return;
        }

        // With the box off no dialog opens at all, so the tab and the status line carry the news.
        if (!_settings.PromptForUpdates)
        {
            if (asked) ShowUpdateLink(manifest);
            return;
        }

        switch (action)
        {
            case UpdateAction.ShowLink:
                ShowUpdateLink(manifest);
                break;

            case UpdateAction.OfferSetup:
                await OfferUpdateAsync(manifest);
                break;
        }
    }

    /// <summary>Takes the new version the last check found. A portable copy is sent to the
    /// page it downloads from instead.</summary>
    private void OnInstallUpdate(object sender, RoutedEventArgs e) => InstallRun = InstallUpdateAsync();

    private async Task InstallUpdateAsync()
    {
        if (_newVersion is not UpdateManifest manifest)
        {
            SetStatus("Press Check now first.");
            return;
        }

        if (UpdateDecision.For(manifest, AppVersion.Code, IsPortableCopy(), dismissedVersionCode: 0)
            == UpdateAction.OfferSetup)
        {
            InstallUpdateButton.IsEnabled = false;

            try
            {
                await OfferUpdateAsync(manifest);
            }
            finally
            {
                InstallUpdateButton.IsEnabled = true;
            }

            return;
        }

        OpenReleasePage(manifest);
    }

    /// <summary>Opens the page a release is downloaded from.</summary>
    private void OpenReleasePage(UpdateManifest manifest)
    {
        var page = string.IsNullOrWhiteSpace(manifest.ReleaseUrl) ? UpdateSource.ReleasesUrl : manifest.ReleaseUrl;
        OpenFolder(page);
        SetStatus($"Opened the page for version {manifest.VersionName}.");
    }

    private void ShowUpdateLink(UpdateManifest manifest)
    {
        var page = string.IsNullOrWhiteSpace(manifest.ReleaseUrl) ? UpdateSource.ReleasesUrl : manifest.ReleaseUrl;
        var link = new Hyperlink(new Run("the release page")) { ToolTip = page };
        link.Click += (_, _) => OpenFolder(page);

        StatusText.Inlines.Clear();
        StatusText.Inlines.Add(new Run($"Version {manifest.VersionName} is available. Download it from "));
        StatusText.Inlines.Add(link);
        StatusText.Inlines.Add(new Run("."));
        RecordStatus($"Version {manifest.VersionName} is available at {page}");
    }

    private async Task OfferUpdateAsync(UpdateManifest manifest)
    {
        var download = manifest.Setup!;
        var size = download.Bytes > 0 ? $" The download is about {download.Bytes / (1024 * 1024)} MB." : string.Empty;

        var question = $"Version {manifest.VersionName} is available. You are running {AppVersion.Name}.{size}"
            + Environment.NewLine + Environment.NewLine
            + "Download it and run the installer now? The program will close while it installs.";

        // Pressing Install update is the answer already when the box has the dialogs turned off.
        var wanted = !_settings.PromptForUpdates || Confirm(question, "Update available");

        if (!wanted)
        {
            // Saying no means this release, not every release after it.
            _settings.DismissedUpdateCode = manifest.VersionCode;
            _settings.Save();
            ShowUpdateLink(manifest);
            return;
        }

        SetStatus($"Downloading version {manifest.VersionName}...");
        var progress = new Progress<UpdateProgress>(p => Progress.Value = p.Fraction * 100);

        using var cancellation = new CancellationTokenSource();
        var (result, setupPath) = await UpdateFetchSetup(download, progress, cancellation.Token);
        Progress.Value = 0;

        if (result == UpdateFetchResult.Ready && setupPath is not null)
        {
            SetStatus("Starting the installer.");
            SetupLauncher(setupPath);
            return;
        }

        SetStatus(DescribeFailure(result, manifest));
    }

    private static string DescribeFailure(UpdateFetchResult result, UpdateManifest manifest) => result switch
    {
        UpdateFetchResult.HashMismatch =>
            "The download did not match its published checksum, so it was thrown away. Try again later.",
        UpdateFetchResult.NoSetupInside =>
            "The download held no installer. Fetch it by hand from the release page.",
        UpdateFetchResult.Cancelled => "The update download was stopped.",
        _ => $"Version {manifest.VersionName} could not be downloaded. Try again later.",
    };
}
