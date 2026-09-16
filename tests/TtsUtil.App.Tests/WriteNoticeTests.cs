using System.IO;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class WriteNoticeTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly MainWindow _window;

    public WriteNoticeTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinNotice", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "voices"));

        _window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = Path.Combine(_root, "voices");
            settings.UseWindowsVoices = false;

            return new MainWindow(settings, loadVoiceOnSelection: false);
        });
    }

    public void Dispose()
    {
        _wpf.Invoke(() => _window.Close());

        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void ThePickedNoticeIsTheOneAFinishedFileGets()
    {
        _wpf.Invoke(() =>
        {
            var seen = new List<WriteFinishedNotice>();
            _window.WriteFinishedNotifier = (notice, _) => seen.Add(notice);

            _window.SettingsWriteNoticeBox.SelectedIndex = 0;
            _window.NotifyWriteFinished(Path.Combine(_root, "one.mp3"));
            Assert.Empty(seen);

            _window.SettingsWriteNoticeBox.SelectedIndex = 2;
            _window.NotifyWriteFinished(Path.Combine(_root, "two.mp3"));

            Assert.Equal(WriteFinishedNotice.PopupAndSound, Assert.Single(seen));
            Assert.Equal(WriteFinishedNotice.PopupAndSound, _window.Settings.WriteFinishedNotice);
        });
    }

    /// <summary>A batch says so once at the end rather than once for every script.</summary>
    [Fact]
    public void NothingIsAnnouncedWhileABatchIsRunning()
    {
        _wpf.Invoke(() =>
        {
            var seen = 0;
            _window.WriteFinishedNotifier = (_, _) => seen++;
            _window.SettingsWriteNoticeBox.SelectedIndex = 1;

            _window.HoldWriteNotices(true);
            _window.NotifyWriteFinished(Path.Combine(_root, "one.mp3"));
            Assert.Equal(0, seen);

            _window.HoldWriteNotices(false);
            _window.NotifyWriteFinished(Path.Combine(_root, "two.mp3"));
            Assert.Equal(1, seen);
        });
    }
}
