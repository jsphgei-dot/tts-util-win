using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class HistoryPagingUiTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly MainWindow _window;

    public HistoryPagingUiTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinHistory", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "voices"));

        _window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = Path.Combine(_root, "voices");
            settings.UseWindowsVoices = false;
            settings.ShowLastMessages = 3;
            settings.KeepLastMessages = 12;

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
    public void ThePageShowsAsManyRowsAsTheSettingAsksFor()
    {
        Fill(7);

        Open();

        _wpf.Invoke(() =>
        {
            Assert.Equal(3, _window.HistoryPageRows.Count);
            Assert.Equal("message 7", _window.HistoryPageRows[0]);
            Assert.Equal(3, _window.HistoryPageCount);
        });
    }

    [Fact]
    public void GoingBackAPageShowsOlderMessages()
    {
        Fill(7);
        Open();

        Click(_window.HistoryOlderButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal(2, _window.HistoryPageNumber);
            Assert.Equal("message 4", _window.HistoryPageRows[0]);
        });
    }

    [Fact]
    public void TheDoubleArrowComesStraightBackToTheNewest()
    {
        Fill(7);
        Open();

        Click(_window.HistoryOlderButton);
        Click(_window.HistoryOlderButton);
        Click(_window.HistoryNewestButton);

        _wpf.Invoke(() => Assert.Equal(1, _window.HistoryPageNumber));
    }

    [Fact]
    public void TheArrowsAreDisabledAtEachEndRatherThanHidden()
    {
        Fill(7);
        Open();

        _wpf.Invoke(() =>
        {
            Assert.False(_window.HistoryNewerButton.IsEnabled);
            Assert.False(_window.HistoryNewestButton.IsEnabled);
            Assert.True(_window.HistoryOlderButton.IsEnabled);
        });
    }

    [Fact]
    public void TheHistoryStopsAtTheNumberTheSettingKeeps()
    {
        Fill(30);

        _wpf.Invoke(() =>
        {
            Assert.Equal(12, _window.StatusHistory.Count);
            Assert.Equal("message 30", _window.StatusHistory[^1]);
        });
    }

    private void Fill(int count) => _wpf.Invoke(() =>
    {
        for (var at = 1; at <= count; at++) _window.SetStatusForTest($"message {at}");
    });

    private void Open() => _wpf.Invoke(() => _window.StatusHistoryButton.IsChecked = true);

    private void Click(ButtonBase button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));
}
