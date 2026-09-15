using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class MainWindowTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _voicesDir;
    private readonly string _settingsPath;
    private readonly List<(string Message, string Title)> _errors = new();
    private readonly List<MainWindow> _windows = new();

    public MainWindowTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinTests", Guid.NewGuid().ToString("N"));
        _voicesDir = Path.Combine(_root, "voices");
        _settingsPath = Path.Combine(_root, "settings.json");
        Directory.CreateDirectory(_voicesDir);
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
            // A leftover temp directory is not worth failing a test over.
        }
    }

    // --- Construction and voice discovery ---

    [Fact]
    public void WindowOpensWithTheExpectedTitleAndTabs()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Equal("TTS Util Win", window.Title);
            var headers = window.Tabs.Items.Cast<TabItem>().Select(t => t.Header.ToString()).ToList();
            Assert.Equal(new[] { "Text", "File", "Voices", "Settings", "About" }, headers);
        });
    }

    [Fact]
    public void AnEmptyVoicesDirectoryProducesGuidanceInTheStatusBar()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Empty(window.Voices);
            Assert.Empty(window.VoiceBox.Items);
            Assert.Contains("No voices found", window.StatusText.Text);
            Assert.Contains("Voices tab", window.StatusText.Text);
        });
    }

    [Fact]
    public void VoicesInTheDirectoryAppearInTheDropDown()
    {
        CreateFakeVoice("test-voice-alpha");

        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Single(window.Voices);
            Assert.Single(window.VoiceBox.Items);
            Assert.Equal("test-voice-alpha  (Vits)", window.VoiceBox.Items[0]);
            Assert.Equal(0, window.VoiceBox.SelectedIndex);
        });
    }

    [Fact]
    public void TheVoiceUsedLastTimeIsPreselected()
    {
        CreateFakeVoice("aaa-first");
        CreateFakeVoice("zzz-second");

        var window = CreateWindow(s => s.LastVoiceName = "zzz-second");

        _wpf.Invoke(() => Assert.Equal(1, window.VoiceBox.SelectedIndex));
    }

    [Fact]
    public void AboutTabReportsTheVoiceDirectoryAndCount()
    {
        CreateFakeVoice("test-voice-alpha");

        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Contains(_voicesDir, window.AboutVoicesText.Text);
            Assert.Contains("1 voice(s) found", window.AboutVoicesText.Text);
        });
    }

    [Fact]
    public void TheAboutTabShowsTheVersion()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Contains(TtsUtil.Core.AppVersion.Name, window.VersionText.Text);
            Assert.Contains("build", window.VersionText.Text);
        });
    }

    [Fact]
    public void RescanPicksUpAVoiceAddedAfterStartup()
    {
        var window = CreateWindow();
        _wpf.Invoke(() => Assert.Empty(window.Voices));

        CreateFakeVoice("added-later");
        Click(window.RescanButton);

        _wpf.Invoke(() => Assert.Single(window.Voices));
    }

    [Fact]
    public void TheWindowRendersItsVisualTree()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(940, 660));
            root.Arrange(new Rect(0, 0, 940, 660));
            root.UpdateLayout();

            var bitmap = new RenderTargetBitmap(940, 660, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(root);

            Assert.Equal(940, bitmap.PixelWidth);
        });
    }

    [Fact]
    public void EveryTabRealisesAndRenders()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(940, 660));
            root.Arrange(new Rect(0, 0, 940, 660));

            for (var index = 0; index < window.Tabs.Items.Count; index++)
            {
                window.Tabs.SelectedIndex = index;
                root.UpdateLayout();

                var bitmap = new RenderTargetBitmap(940, 660, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(root);

                var tab = (TabItem)window.Tabs.Items[index];
                Assert.True(tab.IsSelected, $"Tab {tab.Header} did not become selected.");
                Assert.NotNull(tab.Content);
            }
        });
    }

    [Fact]
    public void TheProcessIsNotInGlobalizationInvariantMode()
    {
        var invariant = AppContext.TryGetSwitch("System.Globalization.Invariant", out var enabled) && enabled;

        Assert.False(invariant);
    }

    [Fact]
    public void TheShippingAppIsNotBuiltInGlobalizationInvariantMode()
    {
        var configPath = Path.ChangeExtension(typeof(MainWindow).Assembly.Location, ".runtimeconfig.json");
        Assert.True(File.Exists(configPath), $"Expected the app runtime config at {configPath}.");

        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        var properties = document.RootElement
            .GetProperty("runtimeOptions")
            .TryGetProperty("configProperties", out var element)
            ? element
            : default;

        if (properties.ValueKind != JsonValueKind.Object) return;

        // WPF resolves en-US (LCID 1033) while formatting text; invariant mode throws there.
        var invariant = properties.TryGetProperty("System.Globalization.Invariant", out var value) &&
                        value.ValueKind == JsonValueKind.True;

        Assert.False(invariant, "TtsUtilWin is built with InvariantGlobalization, which breaks WPF text rendering.");
    }

    // --- Speaker selection ---

    [Fact]
    public void ASingleSpeakerVoiceHidesTheSpeakerPicker()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(1);
            Assert.Equal(Visibility.Collapsed, window.SpeakerBox.Visibility);
            Assert.Equal(Visibility.Collapsed, window.SpeakerLabel.Visibility);
            Assert.Single(window.SpeakerBox.Items);
        });
    }

    [Fact]
    public void AMultiSpeakerVoiceShowsAndFillsTheSpeakerPicker()
    {
        var window = CreateWindow(s => s.SpeakerId = 3);

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(5);
            Assert.Equal(Visibility.Visible, window.SpeakerBox.Visibility);
            Assert.Equal(5, window.SpeakerBox.Items.Count);
            Assert.Equal(3, window.SpeakerBox.SelectedIndex);
        });
    }

    [Fact]
    public void AStoredSpeakerBeyondTheVoiceRangeIsClamped()
    {
        var window = CreateWindow(s => s.SpeakerId = 900);

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(4);
            Assert.Equal(3, window.SpeakerBox.SelectedIndex);
        });
    }

    // --- Text tab actions ---

    [Fact]
    public void ReadingWithNoTextReportsThereIsNothingToRead()
    {
        var window = CreateWindow();

        Click(window.ReadButton);

        _wpf.Invoke(() => Assert.Equal("There is no text to read.", window.StatusText.Text));
    }

    [Fact]
    public void SavingWithNoTextReportsThereIsNothingToWrite()
    {
        var window = CreateWindow();

        Click(window.SaveWaveButton);

        _wpf.Invoke(() => Assert.Equal("There is no text to write.", window.StatusText.Text));
    }

    [Fact]
    public void ReadingTextWithNoVoiceSelectedAsksForAVoice()
    {
        var window = CreateWindow();

        _wpf.Invoke(() => window.InputText.Text = "Hello there.");
        Click(window.ReadButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal("Select a voice first.", window.StatusText.Text);
            Assert.Empty(_errors);
        });
    }

    [Fact]
    public void ClearEmptiesTheInputBox()
    {
        var window = CreateWindow();
        _wpf.Invoke(() => window.InputText.Text = "some text");

        Click(window.ClearButton);

        _wpf.Invoke(() => Assert.Equal(string.Empty, window.InputText.Text));
    }

    [Fact]
    public void StopWithNothingRunningIsHarmless()
    {
        var window = CreateWindow();

        Click(window.StopButton);

        _wpf.Invoke(() => Assert.Equal("Stopping...", window.StatusText.Text));
    }

    [Fact]
    public void TypingWithReadAsYouTypeOffDoesNothing()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.ReadAsYouTypeBox.IsChecked = false;
            window.InputText.Text = "a";
            Assert.Equal("a", window.InputText.Text);
            Assert.Empty(_errors);
        });
    }

    [Fact]
    public void TypingAWordSpeaksItOnlyOnceTheWordIsFinished()
    {
        var window = CreateWindow();
        var spoken = new List<string>();

        _wpf.Invoke(() =>
        {
            window.SpeakSnippet = snippet => spoken.Add(snippet);
            window.ReadAsYouTypeBox.IsChecked = true;

            TypeCharacters(window, "cat");
            Assert.Empty(spoken);

            TypeCharacters(window, " ");
            Assert.Equal(new[] { "cat " }, spoken);
        });
    }

    [Fact]
    public void TypingSeveralWordsSpeaksEachOneInTurn()
    {
        var window = CreateWindow();
        var spoken = new List<string>();

        _wpf.Invoke(() =>
        {
            window.SpeakSnippet = snippet => spoken.Add(snippet);
            window.ReadAsYouTypeBox.IsChecked = true;

            TypeCharacters(window, "the cat sat.");

            Assert.Equal(new[] { "the ", "cat ", "sat." }, spoken);
        });
    }

    [Fact]
    public void RepeatedSpacesDoNotRepeatTheWord()
    {
        var window = CreateWindow();
        var spoken = new List<string>();

        _wpf.Invoke(() =>
        {
            window.SpeakSnippet = snippet => spoken.Add(snippet);
            window.ReadAsYouTypeBox.IsChecked = true;

            TypeCharacters(window, "cat   ");

            Assert.Equal(new[] { "cat " }, spoken);
        });
    }

    [Fact]
    public void TypingSpeaksNothingWhileReadAsYouTypeIsOff()
    {
        var window = CreateWindow();
        var spoken = new List<string>();

        _wpf.Invoke(() =>
        {
            window.SpeakSnippet = snippet => spoken.Add(snippet);
            window.ReadAsYouTypeBox.IsChecked = false;

            TypeCharacters(window, "cat dog ");

            Assert.Empty(spoken);
        });
    }

    [Fact]
    public void ReadAsYouTypeCheckboxIsStoredInSettings()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.ReadAsYouTypeBox.IsChecked = true;
            Assert.True(window.Settings.ReadAsYouType);

            window.ReadAsYouTypeBox.IsChecked = false;
            Assert.False(window.Settings.ReadAsYouType);
        });
    }

    [Fact]
    public void TheSpeedSliderUpdatesTheLabelAndTheSettings()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.SpeedSlider.Value = 1.5;
            Assert.Equal("1.50x", window.SpeedText.Text);
            Assert.Equal(1.5f, window.Settings.Speed);
        });
    }

    // --- File tab actions ---

    [Fact]
    public void ReadingAMissingFileReportsIt()
    {
        var window = CreateWindow();

        _wpf.Invoke(() => window.FilePathBox.Text = Path.Combine(_root, "does-not-exist.txt"));
        Click(window.ReadFileButton);

        _wpf.Invoke(() => Assert.Equal("Choose an existing file first.", window.StatusText.Text));
    }

    [Fact]
    public void ConvertingAMissingFileReportsIt()
    {
        var window = CreateWindow();

        _wpf.Invoke(() => window.FilePathBox.Text = string.Empty);
        Click(window.ConvertFileButton);

        _wpf.Invoke(() => Assert.Equal("Choose an existing file first.", window.StatusText.Text));
    }

    [Fact]
    public void ReadingAnExistingFileWithNoVoiceAsksForAVoice()
    {
        var file = Path.Combine(_root, "input.txt");
        File.WriteAllText(file, "Some text in a file.");
        var window = CreateWindow();

        _wpf.Invoke(() => window.FilePathBox.Text = file);
        Click(window.ReadFileButton);

        _wpf.Invoke(() => Assert.Equal("Select a voice first.", window.StatusText.Text));
    }

    // --- Settings tab ---

    [Fact]
    public void SettingsLoadIntoTheirControls()
    {
        var window = CreateWindow(s =>
        {
            s.SilenceLineEndingMs = 321;
            s.SilenceSentenceMs = 654;
            s.FilterWebLinks = true;
            s.NumThreads = 4;
        });

        _wpf.Invoke(() =>
        {
            Assert.Equal("321", window.SilenceLineEndingBox.Text);
            Assert.Equal("654", window.SilenceSentenceBox.Text);
            Assert.True(window.FilterWebBox.IsChecked);
            Assert.Equal("4", window.ThreadsBox.Text);
            Assert.Equal(_voicesDir, window.VoicesDirBox.Text);
        });
    }

    [Fact]
    public void ApplyingSettingsUpdatesTheSettingsObject()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.SilenceLineEndingBox.Text = "150";
            window.SilenceSentenceBox.Text = "400";
            window.SilenceQuestionBox.Text = "450";
            window.SilenceExclamationBox.Text = "500";
            window.ScaleSilenceBox.IsChecked = true;
            window.FilterHashBox.IsChecked = true;
            window.FilterMailBox.IsChecked = true;
        });

        Click(window.ApplySettingsButton);

        _wpf.Invoke(() =>
        {
            var settings = window.Settings;
            Assert.Equal(150, settings.SilenceLineEndingMs);
            Assert.Equal(400, settings.SilenceSentenceMs);
            Assert.Equal(450, settings.SilenceQuestionMs);
            Assert.Equal(500, settings.SilenceExclamationMs);
            Assert.True(settings.ScaleSilenceToRate);
            Assert.True(settings.FilterHashes);
            Assert.True(settings.FilterMailToLinks);
            Assert.False(settings.FilterWebLinks);
            Assert.Equal("Settings saved.", window.StatusText.Text);
        });
    }

    [Fact]
    public void ApplyingSettingsWritesTheSettingsFile()
    {
        var window = CreateWindow();

        _wpf.Invoke(() => window.SilenceQuestionBox.Text = "777");
        Click(window.ApplySettingsButton);

        Assert.True(File.Exists(_settingsPath));
        var reloaded = AppSettings.LoadFrom(_settingsPath);
        Assert.Equal(777, reloaded.SilenceQuestionMs);
    }

    [Fact]
    public void ApplyClampsThreadsAndChunkLengthToSaneRanges()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.ThreadsBox.Text = "99";
            window.ChunkLengthBox.Text = "1";
        });

        Click(window.ApplySettingsButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal(16, window.Settings.NumThreads);
            Assert.Equal(64, window.Settings.MaxChunkLength);
            Assert.Equal("16", window.ThreadsBox.Text);
            Assert.Equal("64", window.ChunkLengthBox.Text);
        });
    }

    [Fact]
    public void ApplyKeepsThePreviousValueWhenABoxHoldsNonsense()
    {
        var window = CreateWindow(s => s.SilenceLineEndingMs = 200);

        _wpf.Invoke(() => window.SilenceLineEndingBox.Text = "not a number");
        Click(window.ApplySettingsButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal(200, window.Settings.SilenceLineEndingMs);
            Assert.Equal("200", window.SilenceLineEndingBox.Text);
        });
    }

    [Fact]
    public void ChangingTheVoicesDirectoryRescansImmediately()
    {
        var otherDir = Path.Combine(_root, "other-voices");
        Directory.CreateDirectory(otherDir);
        CreateFakeVoice("voice-in-other-dir", otherDir);

        var window = CreateWindow();
        _wpf.Invoke(() => Assert.Empty(window.Voices));

        _wpf.Invoke(() => window.VoicesDirBox.Text = otherDir);
        Click(window.ApplySettingsButton);

        _wpf.Invoke(() =>
        {
            Assert.Single(window.Voices);
            Assert.Equal("voice-in-other-dir", window.Voices[0].Name);
        });
    }

    [Fact]
    public void ClosingTheWindowSavesSettings()
    {
        var window = CreateWindow(s => s.SilenceExclamationMs = 888);

        _wpf.Invoke(() =>
        {
            window.Close();
            _windows.Remove(window);
        });

        Assert.True(File.Exists(_settingsPath));
        Assert.Equal(888, AppSettings.LoadFrom(_settingsPath).SilenceExclamationMs);
    }

    // --- Helpers ---

    private MainWindow CreateWindow(Action<AppSettings>? configure = null)
    {
        var window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(_settingsPath);
            settings.VoicesDirectory = _voicesDir;
            settings.OutputDirectory = _root;
            configure?.Invoke(settings);

            var created = new MainWindow(settings, loadVoiceOnSelection: false)
            {
                ErrorReporter = (message, title) => _errors.Add((message, title)),
            };

            return created;
        });

        _windows.Add(window);
        return window;
    }

    /// <summary>Appends one character at a time, the way a text box reports keystrokes.</summary>
    private static void TypeCharacters(MainWindow window, string text)
    {
        foreach (var c in text) window.InputText.AppendText(c.ToString());
    }

    private void Click(Button button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

    private void CreateFakeVoice(string name, string? parentDirectory = null)
    {
        var directory = Path.Combine(parentDirectory ?? _voicesDir, name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "tokens.txt"), "a 1\nb 2\n");
        File.WriteAllBytes(Path.Combine(directory, $"{name}.onnx"), new byte[64]);
    }
}
