using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Tts;
using Windows.Media;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class PauseTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly List<MainWindow> _windows = new();

    public PauseTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinPause", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "voices"));
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
    public void PressingPauseWithNothingPlayingSaysSo()
    {
        var window = CreateWindow();

        Click(window.PauseButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal("Nothing is playing.", window.StatusText.Text);
            Assert.Equal("Pause", window.PauseButton.ToolTip);
        });
    }

    [Fact]
    public void PauseAndResumeToggleThePlaybackAndBothButtons()
    {
        var playback = new FakePlayback();
        var window = CreateWindow();
        _wpf.Invoke(() => window.ActivePlayback = playback);

        Click(window.PauseButton);

        _wpf.Invoke(() =>
        {
            Assert.True(playback.IsPaused);
            Assert.Equal("Resume", window.PauseButton.ToolTip);
            Assert.Equal("Resume", window.PauseFileButton.ToolTip);
        });

        Click(window.PauseFileButton);

        _wpf.Invoke(() =>
        {
            Assert.False(playback.IsPaused);
            Assert.Equal("Pause", window.PauseButton.ToolTip);
            Assert.Equal("Pause", window.PauseFileButton.ToolTip);
        });
    }

    [Fact]
    public void StoppingWhilePausedResumesFirstSoTheRunCanUnwind()
    {
        var playback = new FakePlayback();
        var window = CreateWindow();
        _wpf.Invoke(() => window.ActivePlayback = playback);

        Click(window.PauseButton);
        Click(window.StopButton);

        Assert.False(playback.IsPaused);
        Assert.True(playback.Stopped);
        _wpf.Invoke(() => Assert.Equal("Pause", window.PauseButton.ToolTip));
    }

    private void Click(ButtonBase button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

    private MainWindow CreateWindow()
    {
        var window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = Path.Combine(_root, "voices");
            settings.OutputDirectory = _root;

            return new MainWindow(settings, loadVoiceOnSelection: false);
        });

        _windows.Add(window);
        return window;
    }

    [Fact]
    public void TheMediaKeysPauseAndResumeTheSameReading()
    {
        var playback = new FakePlayback();
        var window = CreateWindow();
        _wpf.Invoke(() => window.ActivePlayback = playback);

        _wpf.Invoke(() => window.OnMediaButton(SystemMediaTransportControlsButton.Pause));

        _wpf.Invoke(() =>
        {
            Assert.True(playback.IsPaused);
            Assert.Equal("Resume", window.PauseButton.ToolTip);
        });

        _wpf.Invoke(() => window.OnMediaButton(SystemMediaTransportControlsButton.Play));

        _wpf.Invoke(() =>
        {
            Assert.False(playback.IsPaused);
            Assert.Equal("Pause", window.PauseButton.ToolTip);
        });
    }

    [Fact]
    public void TheStopMediaKeyStopsTheRun()
    {
        var playback = new FakePlayback();
        var window = CreateWindow();
        _wpf.Invoke(() => window.ActivePlayback = playback);

        _wpf.Invoke(() => window.OnMediaButton(SystemMediaTransportControlsButton.Stop));

        Assert.True(playback.Stopped);
    }

    private sealed class FakePlayback : IAudioPlayback
    {
        public bool IsPaused { get; private set; }

        public long PlayedCharacters { get; set; }

        public bool Stopped { get; private set; }

        public void Pause() => IsPaused = true;

        public void Resume() => IsPaused = false;

        public void Stop() => Stopped = true;

        public void WaitUntilDrained()
        {
        }

        public void WriteSamples(ReadOnlySpan<float> samples)
        {
        }

        public void WriteSilence(int milliseconds)
        {
        }

        public void Dispose()
        {
        }
    }
}
