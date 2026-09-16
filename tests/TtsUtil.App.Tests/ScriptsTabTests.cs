using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
    public void AudioWrittenFromAnOpenedScriptIsNamedAfterIt()
    {
        File.WriteAllText(Path.Combine(_scriptsDir, "Act One.txt"), "First.");
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.RefreshScripts();
            window.ScriptList.SelectedIndex = 0;
        });

        Click(window.OpenScriptButton);
        _wpf.Invoke(() => Assert.Equal("Act One", MainWindow.AudioStem(window.TextScriptTitle)));

        Click(window.ClearButton);
        _wpf.Invoke(() => Assert.Equal("tts_output", MainWindow.AudioStem(window.TextScriptTitle)));
    }

    [Fact]
    public void WritingAudioKeepsTheTextAsAScriptUnlessTheSettingIsOff()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.AudioPathPicker = _ => Path.Combine(_root, "Scene Two.mp3");
            window.InputText.Text = "The words behind it.";
        });

        Click(window.SaveWaveButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal("The words behind it.", File.ReadAllText(Path.Combine(_scriptsDir, "Scene Two.txt")));
            Assert.Equal("Scene Two", window.TextScriptTitle);

            window.Settings.SaveScriptWithAudio = false;
            window.AudioPathPicker = _ => Path.Combine(_root, "Scene Three.mp3");
        });

        Click(window.SaveWaveButton);

        Assert.False(File.Exists(Path.Combine(_scriptsDir, "Scene Three.txt")));
    }

    /// <summary>Ctrl+Shift+S writes the audio without reaching for the icon.</summary>
    [Fact]
    public void TheKeyboardSaveAudioWritesTheOpenDocument()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.AudioPathPicker = _ => Path.Combine(_root, "Scene Four.mp3");
            window.InputText.Text = "The words behind it.";

            ApplicationCommands.SaveAs.Execute(null, window);

            Assert.Equal("The words behind it.", File.ReadAllText(Path.Combine(_scriptsDir, "Scene Four.txt")));
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
            window.ScriptList.SelectedItem = window.ScriptList.Items.Cast<ScriptRow>()
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

    [Fact]
    public void AScriptKeepsTheVoiceAndSpeedItWasSavedWith()
    {
        var window = CreateWindowWithAVoice();

        _wpf.Invoke(() =>
        {
            window.InputText.Text = "The script body.";
            window.ScriptTitleBox.Text = "Act One";
            window.SpeedSlider.Value = 1.4;
        });

        Click(window.SaveScriptButton);

        _wpf.Invoke(() =>
        {
            window.SpeedSlider.Value = 0.8;
            window.ScriptList.SelectedIndex = 0;
        });

        Click(window.OpenScriptButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal(1.4, window.SpeedSlider.Value, 3);
            Assert.Equal(0, window.VoiceBox.SelectedIndex);
            Assert.Equal("Act One", window.TextScriptTitle);
        });
    }

    /// <summary>A double click opens a script beside what is already open, and the Text tab wears
    /// a mark until it is looked at.</summary>
    [Fact]
    public void DoubleClickingAScriptOpensItInANewTab()
    {
        File.WriteAllText(Path.Combine(_scriptsDir, "Act Two.txt"), "The second act.");
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.RefreshScripts();
            window.Tabs.SelectedIndex = 1;
            window.OpenScriptInNewTab(window.Scripts.List().Single());

            Assert.Equal(2, window.Documents.Count);
            Assert.Equal("Act Two", window.Documents[^1].Title);
            Assert.Equal("The second act.", window.InputText.Text);
            Assert.Equal(Visibility.Visible, window.TextTabMark.Visibility);

            window.Tabs.SelectedIndex = 0;

            Assert.Equal(Visibility.Collapsed, window.TextTabMark.Visibility);
        });
    }

    /// <summary>Several files at once become a tab each, named after the file.</summary>
    [Fact]
    public async Task TakingInSeveralFilesOpensOneTabEach()
    {
        var window = CreateWindow();
        _wpf.Invoke(() => window.BatchFilePicker = () => WriteTwoFiles());

        await _wpf.Invoke(() => window.TakeInFilesAsync(asScripts: false));

        _wpf.Invoke(() =>
        {
            Assert.Equal(3, window.Documents.Count);
            Assert.Equal(new[] { "Chapter one", "Chapter two" },
                window.Documents.Skip(1).Select(d => d.Title).ToArray());
            Assert.Equal("The first one.", window.InputText.Text);
        });
    }

    /// <summary>The same files can go straight to the script library instead.</summary>
    [Fact]
    public async Task TakingInSeveralFilesSavesOneScriptEach()
    {
        var window = CreateWindow();
        _wpf.Invoke(() => window.BatchFilePicker = () => WriteTwoFiles());

        await _wpf.Invoke(() => window.TakeInFilesAsync(asScripts: true));

        Assert.Equal("The first one.", File.ReadAllText(Path.Combine(_scriptsDir, "Chapter one.txt")));
        Assert.Equal("The second one.", File.ReadAllText(Path.Combine(_scriptsDir, "Chapter two.txt")));
        _wpf.Invoke(() => Assert.Equal(2, window.ScriptList.Items.Count));
    }

    private IReadOnlyList<string> WriteTwoFiles()
    {
        var first = Path.Combine(_root, "Chapter one.txt");
        var second = Path.Combine(_root, "Chapter two.txt");
        File.WriteAllText(first, "The first one.");
        File.WriteAllText(second, "The second one.");
        return new[] { first, second };
    }

    private MainWindow CreateWindowWithAVoice()
    {
        var window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(_settingsPath);
            settings.VoicesDirectory = _voicesDir;
            settings.UseWindowsVoices = true;

            var created = new MainWindow(settings, loadVoiceOnSelection: false)
            {
                Scripts = new ScriptLibrary(_scriptsDir),
                WindowsVoiceScanner = () => new[]
                {
                    new TtsUtil.Core.Tts.VoiceDescriptor
                    {
                        Name = "David",
                        Source = TtsUtil.Core.Tts.VoiceSource.Windows,
                        Id = "david",
                    },
                },
            };

            created.VoiceBox.SelectedIndex = 0;
            created.RefreshScripts();
            return created;
        });

        _windows.Add(window);
        return window;
    }

    /// <summary>Ctrl+S asks for the name, and the answer is what the file is called.</summary>
    [Fact]
    public void TheKeyboardSaveAsksForANameFirst()
    {
        var window = CreateWindow();
        var suggested = "unset";

        _wpf.Invoke(() =>
        {
            window.InputText.Text = "The script body.";
            window.ScriptTitleBox.Text = "Chapter one";
            window.ScriptNamePrompt = offered =>
            {
                suggested = offered;
                return "Chapter two";
            };

            window.SaveScriptWithPrompt();
        });

        Assert.Equal("Chapter one", suggested);
        Assert.Equal("The script body.", File.ReadAllText(Path.Combine(_scriptsDir, "Chapter two.txt")));
    }

    [Fact]
    public void ClosingTheNameBoxLeavesTheScriptUnsaved()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.InputText.Text = "The script body.";
            window.ScriptNamePrompt = _ => null;

            window.SaveScriptWithPrompt();

            Assert.Equal("The script was not saved.", window.StatusText.Text);
        });

        Assert.Empty(Directory.GetFiles(_scriptsDir));
    }

    /// <summary>The icon turns green on a new name and blue when a script is written over.</summary>
    [Fact]
    public void TheSaveIconMarksANewScriptApartFromAReplacedOne()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.InputText.Text = "The script body.";
            window.ScriptTitleBox.Text = "Chapter one";
        });

        Click(window.SaveScriptButton);
        _wpf.Invoke(() => Assert.Equal(MainWindow.SavedNewBrush, window.SaveScriptFromTextButton.Foreground));

        Click(window.SaveScriptButton);
        _wpf.Invoke(() =>
        {
            Assert.Equal(MainWindow.SavedOverBrush, window.SaveScriptFromTextButton.Foreground);

            window.ClearSaveConfirmation();
            Assert.NotEqual(MainWindow.SavedOverBrush, window.SaveScriptFromTextButton.Foreground);
        });
    }

    /// <summary>A second press landing before the first has finished is dropped.</summary>
    [Fact]
    public void OnlyOnePressAtATimeIsTakenUp()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.True(window.ClaimClick());
            Assert.False(window.ClaimClick());

            window.ReleaseClick();
            Assert.True(window.ClaimClick());
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
            settings.UseWindowsVoices = false;
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
