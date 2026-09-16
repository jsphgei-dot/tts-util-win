using System.IO;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class SaveResumeTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly MainWindow _window;

    public SaveResumeTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinSaveResume", Guid.NewGuid().ToString("N"));
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
    public async Task SavingWhileReadingStopsThePlaybackAndComesBackAtTheSpokenLine()
    {
        var playback = new FakePlayback { PlayedCharacters = 6 };
        var saved = 0;

        _wpf.Invoke(() =>
        {
            _window.InputText.Text = "One.\nTwo.\nThree.";
            _window.RebuildLineList();
            _window.ActivePlayback = playback;
            _window.ReadingFromText = true;
        });

        await _wpf.Invoke(() => _window.SaveThenResumeAsync(() =>
        {
            saved++;
            return Task.CompletedTask;
        }));

        Assert.True(playback.Stopped);
        Assert.Equal(1, saved);

        // "One." plus its line break is five characters, so the reading comes back at five.
        _wpf.Invoke(() => Assert.Equal(5, _window.LastReadStartOffset));
    }

    [Fact]
    public async Task SavingWithNothingPlayingJustSaves()
    {
        var saved = 0;

        await _wpf.Invoke(() => _window.SaveThenResumeAsync(() =>
        {
            saved++;
            return Task.CompletedTask;
        }));

        Assert.Equal(1, saved);
        _wpf.Invoke(() => Assert.Equal(-1, _window.LastReadStartOffset));
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
