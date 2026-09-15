using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class ScriptsTabTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _voicesDir;
    private readonly string _scriptsDir;
    private readonly string _settingsPath;
    private readonly List<MainWindow> _windows = new();

    public ScriptsTabTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinScripts", Guid.NewGuid().ToString("N"));
        _voicesDir = Path.Combine(_root, "voices");
        _scriptsDir = Path.Combine(_root, "scripts");
        _settingsPath = Path.Combine(_root, "settings.json");
        Directory.CreateDirectory(_voicesDir);
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
    public void SavingFromTheTextTabWritesAFileAndListsIt()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.InputText.Text = "The script body.";
            window.ScriptTitleBox.Text = "Chapter one";
        });

        Click(window.SaveScriptButton);

        Assert.Equal("The script body.", File.ReadAllText(Path.Combine(_scriptsDir, "Chapter one.txt")));
        _wpf.Invoke(() => Assert.Single(window.ScriptList.Items));
    }

    [Fact]
    public void SavingWithoutATitleSaysSoRatherThanWritingAnUnnamedFile()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.InputText.Text = "Some text.";
            window.ScriptTitleBox.Text = "  ";
        });

        Click(window.SaveScriptButton);

        _wpf.Invoke(() => Assert.Equal("Give the script a title first.", window.StatusText.Text));
        Assert.Empty(Directory.GetFiles(_scriptsDir));
    }

    [Fact]
    public void OpeningAScriptReplacesTheTextTabAndItsLineList()
    {
        File.WriteAllText(Path.Combine(_scriptsDir, "Saved.txt"), "First.\nSecond.\nThird.");
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.RefreshScripts();
            window.ScriptList.SelectedIndex = 0;
        });

        Click(window.OpenScriptButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal("First.\nSecond.\nThird.", window.InputText.Text);
            Assert.Equal(3, window.LineList.Items.Count);
            Assert.Equal(0, window.Tabs.SelectedIndex);
        });
    }

    [Fact]
    public void RenamingRefusesWhenTheNewTitleIsTaken()
    {
        File.WriteAllText(Path.Combine(_scriptsDir, "Keep.txt"), "keep");
        File.WriteAllText(Path.Combine(_scriptsDir, "Other.txt"), "other");
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.RefreshScripts();
            window.ScriptList.SelectedItem = window.ScriptList.Items.Cast<SavedScript>()
                .First(s => s.Title == "Other");
            window.ScriptTitleBox.Text = "Keep";
        });

        Click(window.RenameScriptButton);

        _wpf.Invoke(() => Assert.Equal("There is already a script called Keep.", window.StatusText.Text));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(_scriptsDir, "Keep.txt")));
    }

    [Fact]
    public void DeletingAsksFirstAndLeavesTheFileWhenTheAnswerIsNo()
    {
        File.WriteAllText(Path.Combine(_scriptsDir, "Doomed.txt"), "text");
        var window = CreateWindow(confirm: false);

        _wpf.Invoke(() =>
        {
            window.RefreshScripts();
            window.ScriptList.SelectedIndex = 0;
        });

        Click(window.DeleteScriptButton);

        Assert.True(File.Exists(Path.Combine(_scriptsDir, "Doomed.txt")));
    }

    [Fact]
    public void DeletingRemovesTheFileWhenConfirmed()
    {
        File.WriteAllText(Path.Combine(_scriptsDir, "Doomed.txt"), "text");
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.RefreshScripts();
            window.ScriptList.SelectedIndex = 0;
        });

        Click(window.DeleteScriptButton);

        Assert.False(File.Exists(Path.Combine(_scriptsDir, "Doomed.txt")));
        _wpf.Invoke(() => Assert.Empty(window.ScriptList.Items));
    }

    [Fact]
    public void SelectingAScriptShowsItWithoutTouchingTheTextTab()
    {
        File.WriteAllText(Path.Combine(_scriptsDir, "Preview me.txt"), "the body");
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.InputText.Text = "unrelated work in progress";
            window.RefreshScripts();
            window.ScriptList.SelectedIndex = 0;

            Assert.Equal("the body", window.ScriptPreview.Text);
            Assert.Equal("Preview me", window.ScriptTitleBox.Text);
            Assert.Equal("unrelated work in progress", window.InputText.Text);
        });
    }

    private void Click(ButtonBase button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

    private MainWindow CreateWindow(bool confirm = true)
    {
        var window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(_settingsPath);
            settings.VoicesDirectory = _voicesDir;
            settings.OutputDirectory = _root;

            var created = new MainWindow(settings, loadVoiceOnSelection: false)
            {
                Scripts = new ScriptLibrary(_scriptsDir),
                Confirmer = (_, _) => confirm,
            };

            created.RefreshScripts();
            return created;
        });

        _windows.Add(window);
        return window;
    }
}
