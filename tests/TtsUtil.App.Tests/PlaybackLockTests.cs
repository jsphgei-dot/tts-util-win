using System.IO;
using System.Windows.Controls;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class PlaybackLockTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly MainWindow _window;

    public PlaybackLockTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinLocks", Guid.NewGuid().ToString("N"));
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

    /// <summary>Changing the voice mid run disposed the engine underneath it, so the settings a
    /// run has already taken a copy of are held still and say why on hover.</summary>
    [Fact]
    public void TheVoiceCannotBeChangedWhileSomethingIsBeingRead()
    {
        _wpf.Invoke(() =>
        {
            var rescanTip = _window.RescanButton.ToolTip;

            _window.LockPlaybackSettings(true);

            Assert.False(_window.VoiceBox.IsEnabled);
            Assert.False(_window.SpeakerBox.IsEnabled);
            Assert.False(_window.SpeedSlider.IsEnabled);
            Assert.Equal(MainWindow.LockedWhilePlaying, _window.VoiceBox.ToolTip);
            Assert.True(ToolTipService.GetShowOnDisabled(_window.VoiceBox));

            _window.LockPlaybackSettings(false);

            Assert.True(_window.VoiceBox.IsEnabled);
            Assert.Equal(rescanTip, _window.RescanButton.ToolTip);
        });
    }
}
