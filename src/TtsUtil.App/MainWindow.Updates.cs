/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using TtsUtil.Core;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Update;

namespace TtsUtil.App;

/// <summary>
/// The once a day look at the published manifest. A portable copy is pointed at the release
/// page; an installed copy can be offered the setup program, which upgrades in place.
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

    /// <summary>Looks for a newer release, at most once a day, and never in a way that blocks.</summary>
    internal void StartUpdateCheck(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (!UpdateChecker.IsDue(_settings.CheckForUpdates, _settings.LastUpdateCheckUtc, now)) return;

        UpdateCheck = RunUpdateCheckAsync(now);
    }

    private async Task RunUpdateCheckAsync(DateTime nowUtc)
    {
        UpdateManifest? manifest;

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            manifest = await UpdateFetcher(timeout.Token);
        }
        catch (Exception)
        {
            // An update check is a courtesy. Failing one is not worth telling anybody about.
            return;
        }

        if (manifest is null) return;

        _settings.LastUpdateCheckUtc = nowUtc;
        _settings.Save();

        var action = UpdateDecision.For(manifest, AppVersion.Code, IsPortableCopy(), _settings.DismissedUpdateCode);

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

        var wanted = Confirm(question, "Update available");

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
