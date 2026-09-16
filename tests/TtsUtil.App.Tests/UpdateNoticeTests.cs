using System.IO;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Update;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class UpdateNoticeTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly List<string> _opened = new();
    private int _setupFetches;

    public UpdateNoticeTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinUpdateNotice", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "voices"));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task APortableCopyIsGivenTheLinkAndDownloadsNothing()
    {
        var window = CreateWindow(portable: true, accept: true);

        await RunCheck(window);

        _wpf.Invoke(() =>
        {
            Assert.Contains("0.9.0-beta is available", window.StatusHistory[^1]);
            Assert.Equal(0, _setupFetches);
        });
    }

    [Fact]
    public async Task SayingNoRemembersThatVersionRatherThanEveryVersion()
    {
        var window = CreateWindow(portable: false, accept: false);

        await RunCheck(window);

        _wpf.Invoke(() =>
        {
            Assert.Equal(0, _setupFetches);
            Assert.Equal(9, window.Settings.DismissedUpdateCode);
            Assert.NotNull(window.Settings.LastUpdateCheckUtc);
        });
    }

    [Fact]
    public async Task AnInstalledCopyThatAgreesFetchesTheSetupAndStartsIt()
    {
        var window = CreateWindow(portable: false, accept: true);

        await RunCheck(window);

        _wpf.Invoke(() =>
        {
            Assert.Equal(1, _setupFetches);
            Assert.Equal(new[] { "setup.exe" }, _opened);
        });
    }

    private async Task RunCheck(MainWindow window)
    {
        _wpf.Invoke(() => window.StartUpdateCheck());
        await window.UpdateCheck;
    }

    private MainWindow CreateWindow(bool portable, bool accept) => _wpf.Invoke(() =>
    {
        var settings = AppSettings.LoadFrom(Path.Combine(_root, Guid.NewGuid().ToString("N") + ".json"));
        settings.VoicesDirectory = Path.Combine(_root, "voices");
        settings.OutputDirectory = _root;

        var window = new MainWindow(settings, loadVoiceOnSelection: false)
        {
            IsPortableCopy = () => portable,
            Confirmer = (_, _) => accept,
            UpdateFetcher = _ => Task.FromResult<UpdateManifest?>(Manifest()),
            SetupLauncher = path => _opened.Add(Path.GetFileName(path)),
        };

        window.UpdateFetchSetup = (_, _, _) =>
        {
            _setupFetches++;
            return Task.FromResult((UpdateFetchResult.Ready, (string?)"C:\\temp\\setup.exe"));
        };

        return window;
    });

    private static UpdateManifest Manifest() => new()
    {
        VersionName = "0.9.0-beta",
        VersionCode = 9,
        ReleaseUrl = "https://example.invalid/releases/tag/v0.9.0-beta",
        Setup = new UpdateDownload
        {
            Url = "https://example.invalid/setup.zip",
            Bytes = 60 * 1024 * 1024,
            Sha256 = new string('a', 64),
        },
    };
}
