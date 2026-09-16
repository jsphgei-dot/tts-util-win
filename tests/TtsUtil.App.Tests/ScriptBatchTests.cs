using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class ScriptBatchTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _scriptsDir;
    private readonly List<MainWindow> _windows = new();

    public ScriptBatchTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinBatch", Guid.NewGuid().ToString("N"));
        _scriptsDir = Path.Combine(_root, "scripts");
        Directory.CreateDirectory(Path.Combine(_root, "voices"));
        Directory.CreateDirectory(_scriptsDir);
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
    public void ConvertingWithNothingTickedSaysSoAndAsksForNoFolder()
    {
        Write("First", "one");
        var window = CreateWindow();
        var asked = 0;
        _wpf.Invoke(() => window.BatchFolderPicker = () =>
        {
            asked++;
            return null;
        });

        Click(window.ConvertScriptsButton);

        Assert.Equal(0, asked);
        _wpf.Invoke(() => Assert.Equal("Tick the scripts you want to convert first.", window.StatusText.Text));
    }

    [Fact]
    public void TickAllChoosesEveryScriptAndClearPutsThemBack()
    {
        Write("First", "one");
        Write("Second", "two");
        var window = CreateWindow();

        Click(window.TickEveryScriptButton);
        _wpf.Invoke(() => Assert.Equal(2, window.ChosenScripts().Count));

        Click(window.ClearScriptTicksButton);
        _wpf.Invoke(() => Assert.Empty(window.ChosenScripts()));
    }

    [Fact]
    public void ATickSurvivesTheListBeingReloaded()
    {
        Write("First", "one");
        Write("Second", "two");
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.ScriptList.Items.Cast<ScriptRow>().First(r => r.Title == "Second").Chosen = true;
            window.RefreshScripts();

            Assert.Equal(new[] { "Second" }, window.ChosenScripts().Select(s => s.Title));
        });
    }

    [Fact]
    public async Task ConvertingTakesTheTickedScriptsAndTheChosenFolder()
    {
        Write("First", "one");
        Write("Second", "two");
        var window = CreateWindow();
        var folder = Path.Combine(_root, "audio");
        Directory.CreateDirectory(folder);

        Click(window.TickEveryScriptButton);
        _wpf.Invoke(() => window.BatchFolderPicker = () => folder);

        Click(window.ConvertScriptsButton);
        await _wpf.Invoke(() => window.PendingWork);

        // No voice is installed, so every file fails, which is still the whole loop.
        _wpf.Invoke(() => Assert.Equal("Nothing was written.", window.StatusText.Text));
    }

    private void Write(string title, string text) =>
        File.WriteAllText(Path.Combine(_scriptsDir, title + ".txt"), text);

    private void Click(ButtonBase button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

    private MainWindow CreateWindow()
    {
        var window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = Path.Combine(_root, "voices");
            settings.UseWindowsVoices = false;
            settings.OutputDirectory = _root;

            var created = new MainWindow(settings, loadVoiceOnSelection: false)
            {
                Scripts = new ScriptLibrary(_scriptsDir),
            };

            created.RefreshScripts();
            return created;
        });

        _windows.Add(window);
        return window;
    }
}
