using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class VoicesTabTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _voicesDir;
    private readonly string _settingsPath;
    private readonly List<MainWindow> _windows = new();

    public VoicesTabTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinVoiceTab", Guid.NewGuid().ToString("N"));
        _voicesDir = Path.Combine(_root, "voices");
        _settingsPath = Path.Combine(_root, "settings.json");
        Directory.CreateDirectory(_voicesDir);
    }

    public void Dispose()
    {
        _wpf.Invoke(() =>
        {
            foreach (var window in _windows) window.Close();
        });

        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void TheVoicesTabListsTheWholeCatalogue()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            var rows = window.VoiceCatalogueList.Items.Cast<VoiceCatalogueRow>().ToList();

            Assert.Equal(DownloadableVoices.All.Count, rows.Count);
            Assert.All(rows, row => Assert.Equal("Not installed", row.Status));
            Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Licence)));
            Assert.Contains(rows, row => row.Size == "305 MB");
        });
    }

    [Fact]
    public void AnInstalledVoiceShowsAsInstalled()
    {
        CreateFakeVoice(DownloadableVoices.All[0].Id);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            var row = Rows(window).First(r => r.Id == DownloadableVoices.All[0].Id);
            Assert.Equal("Installed", row.Status);
        });
    }

    [Fact]
    public void InstallingWithoutASelectionSaysSo()
    {
        var window = CreateWindow();

        Click(window.InstallVoiceButton);

        _wpf.Invoke(() => Assert.Equal("Select a voice to install.", window.VoiceInstallStatus.Text));
    }

    [Fact]
    public void RemovingWithoutASelectionSaysSo()
    {
        var window = CreateWindow();

        Click(window.RemoveVoiceButton);

        _wpf.Invoke(() => Assert.Equal("Select a voice to remove.", window.VoiceInstallStatus.Text));
    }

    [Fact]
    public void CancellingWhenIdleSaysSo()
    {
        var window = CreateWindow();

        Click(window.CancelVoiceButton);

        _wpf.Invoke(() => Assert.Equal("Nothing is installing.", window.VoiceInstallStatus.Text));
    }

    [Fact]
    public void InstallingAVoiceUnpacksItAndUpdatesTheRow()
    {
        var voice = DownloadableVoices.All[0];
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.VoiceInstallerFactory = () => new VoiceInstaller(
                new StubDownloader(),
                new StubExtractor(() => CreateFakeVoice(voice.Id)),
                _root);

            window.VoiceCatalogueList.SelectedIndex = 0;
        });

        Click(window.InstallVoiceButton);
        WaitUntil(window, () => _wpf.Invoke(() => window.VoiceInstallStatus.Text.Contains("installed", StringComparison.OrdinalIgnoreCase)));

        _wpf.Invoke(() =>
        {
            Assert.Equal("Installed", Rows(window).First(r => r.Id == voice.Id).Status);
            Assert.True(window.VoiceProgress.Value == 100, $"progress was {window.VoiceProgress.Value}, status '{window.VoiceInstallStatus.Text}'");
            Assert.Contains(voice.Licence, window.VoiceInstallStatus.Text, StringComparison.Ordinal);
            Assert.Contains(window.Voices, v => v.Name == voice.Id);
        });
    }

    [Fact]
    public void AFailedInstallReportsTheReasonAndLeavesNothingBehind()
    {
        var voice = DownloadableVoices.All[0];
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.VoiceInstallerFactory = () => new VoiceInstaller(
                new StubDownloader(),
                new StubExtractor(() => throw new InvalidOperationException("tar exploded")),
                _root);

            window.VoiceCatalogueList.SelectedIndex = 0;
        });

        Click(window.InstallVoiceButton);
        WaitUntil(window, () => _wpf.Invoke(() => window.VoiceInstallStatus.Text.Contains("failed", StringComparison.OrdinalIgnoreCase)));

        _wpf.Invoke(() =>
        {
            Assert.Contains("tar exploded", window.VoiceInstallStatus.Text, StringComparison.Ordinal);
            Assert.Equal("Not installed", Rows(window).First(r => r.Id == voice.Id).Status);
            Assert.False(Directory.Exists(Path.Combine(_voicesDir, voice.Id)));
            Assert.True(window.InstallVoiceButton.IsEnabled);
            Assert.False(window.CancelVoiceButton.IsEnabled);
        });
    }

    [Fact]
    public void InstallingAVoiceThatIsAlreadyThereIsRefused()
    {
        var voice = DownloadableVoices.All[0];
        CreateFakeVoice(voice.Id);
        var window = CreateWindow();

        _wpf.Invoke(() => window.VoiceCatalogueList.SelectedIndex = 0);
        Click(window.InstallVoiceButton);

        _wpf.Invoke(() =>
            Assert.Contains("already installed", window.VoiceInstallStatus.Text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RemovingAVoiceDeletesItAndRefreshesThePicker()
    {
        var voice = DownloadableVoices.All[0];
        CreateFakeVoice(voice.Id);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Contains(window.Voices, v => v.Name == voice.Id);
            window.VoiceCatalogueList.SelectedIndex = 0;
        });

        Click(window.RemoveVoiceButton);

        _wpf.Invoke(() =>
        {
            Assert.False(Directory.Exists(Path.Combine(_voicesDir, voice.Id)));
            Assert.Equal("Not installed", Rows(window).First(r => r.Id == voice.Id).Status);
            Assert.DoesNotContain(window.Voices, v => v.Name == voice.Id);
        });
    }

    [Fact]
    public void RemovingAVoiceThatIsNotThereSaysSo()
    {
        var window = CreateWindow();

        _wpf.Invoke(() => window.VoiceCatalogueList.SelectedIndex = 0);
        Click(window.RemoveVoiceButton);

        _wpf.Invoke(() =>
            Assert.Contains("not installed", window.VoiceInstallStatus.Text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheTargetDirectoryIsShownAndIsTheConfiguredOne()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Equal(_voicesDir, window.VoiceInstallDirectory);
            Assert.Contains(_voicesDir, window.VoiceTargetText.Text, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static List<VoiceCatalogueRow> Rows(MainWindow window) =>
        window.VoiceCatalogueList.Items.Cast<VoiceCatalogueRow>().ToList();

    private MainWindow CreateWindow()
    {
        var window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(_settingsPath);
            settings.VoicesDirectory = _voicesDir;
            settings.UseWindowsVoices = false;
            settings.OutputDirectory = _root;

            return new MainWindow(settings, loadVoiceOnSelection: false);
        });

        _windows.Add(window);
        return window;
    }

    private void Click(Button button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

    private void CreateFakeVoice(string name)
    {
        var directory = Path.Combine(_voicesDir, name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "tokens.txt"), "a 1");
        File.WriteAllBytes(Path.Combine(directory, "model.onnx"), new byte[64]);
    }

    private void WaitUntil(MainWindow window, Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (condition()) return;
            Thread.Sleep(10);
        }

        var status = _wpf.Invoke(() => window.VoiceInstallStatus.Text);
        Assert.True(condition(), $"The condition never became true. Status was '{status}'.");
    }

    private sealed class StubDownloader : IVoiceDownloader
    {
        public Task DownloadAsync(string url, string destinationPath, IProgress<VoiceInstallProgress>? progress,
            CancellationToken cancellationToken)
        {
            File.WriteAllBytes(destinationPath, new byte[32]);

            progress?.Report(new VoiceInstallProgress
            {
                Phase = VoiceInstallPhase.Downloading,
                BytesReceived = 32,
                TotalBytes = 32,
            });

            return Task.CompletedTask;
        }
    }

    private sealed class StubExtractor : IArchiveExtractor
    {
        private readonly Action _onExtract;

        public StubExtractor(Action onExtract) => _onExtract = onExtract;

        public Task ExtractAsync(string archivePath, string destinationDirectory,
            CancellationToken cancellationToken)
        {
            _onExtract();
            return Task.CompletedTask;
        }
    }
}
