using System.IO;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class RestartTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly MainWindow _window;

    public RestartTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinRestart", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "voices"));

        _window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = Path.Combine(_root, "voices");
            settings.UseWindowsVoices = false;
            settings.OutputDirectory = _root;

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
    public async Task RestartStopsFirstAndWaitsForTheRunToUnwind()
    {
        var playback = new StubPlayback();
        var run = new TaskCompletionSource();
        var started = 0;

        _wpf.Invoke(() =>
        {
            _window.ActivePlayback = playback;
            _window.CurrentRun = run.Task;
        });

        var restart = _wpf.Invoke(() => _window.RestartAsync(() => started++));

        Assert.True(playback.Stopped);
        _wpf.Invoke(() => Assert.Equal("Restarting...", _window.StatusText.Text));
        Assert.Equal(0, started);

        run.SetResult();
        await restart;

        Assert.Equal(1, started);
    }

    [Fact]
    public async Task PressingRestartTwiceStartsOneReadingRatherThanTwo()
    {
        var run = new TaskCompletionSource();
        var started = 0;

        _wpf.Invoke(() =>
        {
            _window.ActivePlayback = new StubPlayback();
            _window.CurrentRun = run.Task;
        });

        var first = _wpf.Invoke(() => _window.RestartAsync(() => started++));
        var second = _wpf.Invoke(() => _window.RestartAsync(() => started++));

        run.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(1, started);
    }

    private sealed class StubPlayback : IAudioPlayback
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
