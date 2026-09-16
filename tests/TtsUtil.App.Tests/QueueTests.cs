using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class QueueTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _scriptsDir;
    private readonly MainWindow _window;
    private readonly List<string> _played = new();

    public QueueTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinQueue", Guid.NewGuid().ToString("N"));
        _scriptsDir = Path.Combine(_root, "scripts");
        Directory.CreateDirectory(Path.Combine(_root, "voices"));
        Directory.CreateDirectory(_scriptsDir);

        _window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = Path.Combine(_root, "voices");
            settings.OutputDirectory = _root;

            return new MainWindow(settings, loadVoiceOnSelection: false)
            {
                Scripts = new ScriptLibrary(_scriptsDir),
            };
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
    public void EntriesPlayInOrderAndTheQueueStopsAtTheEnd()
    {
        var entries = Entries("One", "Two", "Three");
        PlayWith(_ => RunOutcome.Finished);

        _wpf.Invoke(() => _window.PlaySequenceAsync(entries, 0)).Wait();

        Assert.Equal(new[] { "One", "Two", "Three" }, _played);
    }

    [Fact]
    public void RepeatAllWrapsRoundUntilItIsStopped()
    {
        var entries = Entries("One", "Two");
        SetRepeat(RepeatMode.All);

        // Stopping on the fifth play is what a listener pressing Stop does.
        PlayWith(_ => _played.Count >= 5 ? RunOutcome.Stopped : RunOutcome.Finished);

        _wpf.Invoke(() => _window.PlaySequenceAsync(entries, 0)).Wait();

        Assert.Equal(new[] { "One", "Two", "One", "Two", "One" }, _played);
    }

    [Fact]
    public void RepeatOneStaysOnTheEntryItStartedFrom()
    {
        var entries = Entries("One", "Two", "Three");
        SetRepeat(RepeatMode.One);
        PlayWith(_ => _played.Count >= 3 ? RunOutcome.Stopped : RunOutcome.Finished);

        _wpf.Invoke(() => _window.PlaySequenceAsync(entries, 1)).Wait();

        Assert.Equal(new[] { "Two", "Two", "Two" }, _played);
    }

    [Fact]
    public void AQueueOfUnreadableEntriesGivesUpRatherThanSpinning()
    {
        var entries = Entries("One", "Two");
        SetRepeat(RepeatMode.All);
        PlayWith(_ => RunOutcome.Failed);

        var finished = _wpf.Invoke(() => _window.PlaySequenceAsync(entries, 0)).Wait(TimeSpan.FromSeconds(5));

        Assert.True(finished, "repeat all spun on entries that could not be read");
        Assert.Equal(new[] { "One", "Two" }, _played);
    }

    [Fact]
    public void AddingUsesTheSelectedScriptAndTheButtonsReorderTheQueue()
    {
        File.WriteAllText(Path.Combine(_scriptsDir, "First.txt"), "one");
        File.WriteAllText(Path.Combine(_scriptsDir, "Second.txt"), "two");

        _wpf.Invoke(() =>
        {
            _window.RefreshScripts();
            _window.ScriptList.SelectedItem = _window.ScriptList.Items.Cast<SavedScript>()
                .First(s => s.Title == "First");
        });
        Click(_window.AddToQueueButton);

        _wpf.Invoke(() => _window.ScriptList.SelectedItem = _window.ScriptList.Items.Cast<SavedScript>()
            .First(s => s.Title == "Second"));
        Click(_window.AddToQueueButton);

        _wpf.Invoke(() => _window.QueueList.SelectedIndex = 1);
        Click(_window.MoveQueueUpButton);

        _wpf.Invoke(() => Assert.Equal(new[] { "Second", "First" }, _window.Queue.Select(e => e.Title)));

        Click(_window.RemoveFromQueueButton);

        _wpf.Invoke(() => Assert.Equal(new[] { "First" }, _window.Queue.Select(e => e.Title)));
    }

    [Fact]
    public void WithNoScriptSelectedTheTextTabIsQueued()
    {
        Click(_window.AddToQueueButton);

        _wpf.Invoke(() => Assert.Null(_window.Queue.Single().ScriptTitle));
    }

    private IReadOnlyList<QueueEntry> Entries(params string[] titles) =>
        titles.Select(QueueEntry.ForScript).ToList();

    private void SetRepeat(RepeatMode mode) => _wpf.Invoke(() => _window.Settings.Repeat = mode);

    private void PlayWith(Func<QueueEntry, RunOutcome> outcome) =>
        _wpf.Invoke(() => _window.EntryPlayer = entry =>
        {
            _played.Add(entry.Title);
            return Task.FromResult(outcome(entry));
        });

    private void Click(ButtonBase button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));
}
