using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class StatusHistoryTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly List<MainWindow> _windows = new();

    public StatusHistoryTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinHistory", Guid.NewGuid().ToString("N"));
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
    public void MessagesTheBarOverwritesAreStillInTheHistory()
    {
        var window = CreateWindow();

        Click(window.ReadButton);
        Click(window.SaveWaveButton);
        Click(window.PauseButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal("Nothing is playing.", window.StatusText.Text);
            Assert.Contains("There is no text to read.", window.StatusHistory);
            Assert.Contains("Nothing is playing.", window.StatusHistory);
        });
    }

    [Fact]
    public void ARepeatedMessageIsNotRecordedTwice()
    {
        var window = CreateWindow();

        Click(window.ReadButton);
        Click(window.ReadButton);

        _wpf.Invoke(() =>
            Assert.Equal(1, window.StatusHistory.Count(m => m == "There is no text to read.")));
    }

    [Fact]
    public void TheHistoryStopsGrowingAtItsLimit()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            for (var i = 0; i < MainWindow.StatusHistoryLimit + 20; i++)
            {
                window.InputText.Text = string.Empty;
                window.ReadButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                window.PauseButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }

            Assert.Equal(MainWindow.StatusHistoryLimit, window.StatusHistory.Count);
        });
    }

    [Fact]
    public void OpeningTheHistoryListsTheNewestFirst()
    {
        var window = CreateWindow();

        Click(window.ReadButton);
        Click(window.PauseButton);

        _wpf.Invoke(() =>
        {
            // Whether the popup stays on screen is WPF's business, and a window that was
            // never shown cannot hold one open. What matters is the list it is given.
            window.StatusHistoryButton.IsChecked = true;

            Assert.Equal("Nothing is playing.", window.HistoryPageRows[0]);
            Assert.Equal("There is no text to read.", window.HistoryPageRows[1]);
        });
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
}
