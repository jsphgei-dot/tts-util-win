using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class SettingsMirrorTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _path;
    private readonly MainWindow _window;

    public SettingsMirrorTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinMirror", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "voices"));
        _path = Path.Combine(_root, "settings.json");

        _window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(_path);
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

    /// <summary>Repeat is cycled from the Text tab as well as the queue, and both buttons show
    /// the mode that is on.</summary>
    [Fact]
    public void TheRepeatButtonOnTheTextTabCyclesTheSameMode()
    {
        _wpf.Invoke(() =>
        {
            _window.Settings.Repeat = RepeatMode.Off;
            _window.TextRepeatButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            Assert.Equal(RepeatMode.All, _window.Settings.Repeat);
            Assert.Equal(_window.RepeatModeButton.Content, _window.TextRepeatButton.Content);

            _window.TextRepeatButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            Assert.Equal(RepeatMode.One, _window.Settings.Repeat);
            Assert.Equal(_window.RepeatModeButton.Content, _window.TextRepeatButton.Content);
        });
    }

    /// <summary>With the setting on, closing puts the window in the notification area and leaves
    /// the program running, so a reading carries on.</summary>
    [Fact]
    public void ClosingCanHideTheWindowRatherThanEndTheProgram()
    {
        _wpf.Invoke(() =>
        {
            _window.Show();
            _window.Settings.CloseToTray = true;
            _window.Close();

            Assert.True(_window.InTray);
            Assert.False(_window.IsVisible);

            _window.RestoreFromTray();

            Assert.False(_window.InTray);
            Assert.True(_window.IsVisible);

            _window.Settings.CloseToTray = false;
            _window.Hide();
        });
    }

    [Fact]
    public void TickingASettingOnEitherTabMovesTheOther()
    {
        _wpf.Invoke(() =>
        {
            _window.SettingsPauseWhenUnfocusedBox.IsChecked = true;
            Assert.True(_window.PauseWhenUnfocusedBox.IsChecked);
            Assert.True(_window.Settings.PauseWhenUnfocused);

            _window.PauseWhenUnfocusedBox.IsChecked = false;
            Assert.False(_window.SettingsPauseWhenUnfocusedBox.IsChecked);
            Assert.False(_window.Settings.PauseWhenUnfocused);
        });
    }

    /// <summary>Losing focus only holds the reading once the setting asks for it.</summary>
    [Fact]
    public void LosingFocusPausesOnlyWhenTheSettingIsOn()
    {
        var playback = new StubPlayback();

        _wpf.Invoke(() =>
        {
            _window.ActivePlayback = playback;

            _window.HoldForLostFocus();
            Assert.False(playback.IsPaused);

            _window.PauseWhenUnfocusedBox.IsChecked = true;
            _window.HoldForLostFocus();
            Assert.True(playback.IsPaused);
        });
    }

    [Fact]
    public void ResettingPutsTheBoxesOnBothTabsBack()
    {
        _wpf.Invoke(() =>
        {
            _window.Confirmer = (_, _) => true;
            _window.PauseWhenUnfocusedBox.IsChecked = true;
            _window.ReadAsYouTypeBox.IsChecked = true;

            _window.ResetSettingsButton.RaiseEvent(
                new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

            Assert.False(_window.Settings.PauseWhenUnfocused);
            Assert.False(_window.PauseWhenUnfocusedBox.IsChecked);
            Assert.False(_window.SettingsPauseWhenUnfocusedBox.IsChecked);
            Assert.False(_window.SettingsReadAsYouTypeBox.IsChecked);
        });
    }

    private sealed class StubPlayback : IAudioPlayback
    {
        public bool IsPaused { get; private set; }

        public long PlayedCharacters => 0;

        public void Pause() => IsPaused = true;

        public void Resume() => IsPaused = false;

        public void Stop()
        {
        }

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
