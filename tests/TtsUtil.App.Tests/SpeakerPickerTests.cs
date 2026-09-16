using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class SpeakerPickerTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _voicesDir;
    private readonly string _settingsPath;
    private readonly List<MainWindow> _windows = new();

    public SpeakerPickerTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinSpeakers", Guid.NewGuid().ToString("N"));
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
        }
    }

    [Fact]
    public void ANamedVoiceShowsNumbersAndNamesTogether()
    {
        var voice = WriteVoice("vctk", 3, new Dictionary<int, string> { [0] = "p225", [1] = "p226", [2] = "p227" });
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 3);

            var labels = window.SpeakerBox.Items.Cast<SpeakerInfo>().Select(s => s.Label).ToList();
            Assert.Equal(new[] { "0  p225", "1  p226", "2  p227" }, labels);
        });
    }

    [Fact]
    public void AnUnnamedVoiceStillShowsNumbers()
    {
        var voice = WriteVoice("plain", 4, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 4);

            var labels = window.SpeakerBox.Items.Cast<SpeakerInfo>().Select(s => s.Label).ToList();
            Assert.Equal(new[] { "0", "1", "2", "3" }, labels);
        });
    }

    [Fact]
    public void SearchingNarrowsTheListAndSaysHowMany()
    {
        var voice = WriteVoice("vctk", 3, new Dictionary<int, string> { [0] = "Alice", [1] = "Bob", [2] = "Alicia" });
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 3);
            Assert.Equal("3 speakers", window.SpeakerCountText.Text);

            window.SpeakerSearchBox.Text = "ali";

            var ids = window.SpeakerBox.Items.Cast<SpeakerInfo>().Select(s => s.Id).ToList();
            Assert.Equal(new[] { 0, 2 }, ids);
            Assert.Equal("2 of 3 speakers", window.SpeakerCountText.Text);
        });
    }

    [Fact]
    public void ClearingTheSearchBringsEverySpeakerBack()
    {
        var voice = WriteVoice("plain", 30, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 30);
            window.SpeakerSearchBox.Text = "2";
            Assert.True(window.SpeakerBox.Items.Count < 30);

            window.SpeakerSearchBox.Text = string.Empty;
            Assert.Equal(30, window.SpeakerBox.Items.Count);
        });
    }

    [Fact]
    public void TheSelectedSpeakerSurvivesASearchThatHidesIt()
    {
        var voice = WriteVoice("plain", 30, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 30);
            window.SpeakerBox.SelectedItem = window.SpeakerBox.Items.Cast<SpeakerInfo>().First(s => s.Id == 7);
            Assert.Equal(7, window.SelectedSpeakerId);

            window.SpeakerSearchBox.Text = "29";

            Assert.Equal(-1, window.SpeakerBox.SelectedIndex);
            Assert.Equal(7, window.SelectedSpeakerId);
            Assert.Contains("still using 7", window.SpeakerCountText.Text);
        });
    }

    [Fact]
    public void StarringMovesASpeakerToTheTopOfTheList()
    {
        var voice = WriteVoice("plain", 10, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 10);
            window.SpeakerBox.SelectedItem = window.SpeakerBox.Items.Cast<SpeakerInfo>().First(s => s.Id == 6);
            Click(window.FavouriteSpeakerButton);

            var ids = window.SpeakerBox.Items.Cast<SpeakerInfo>().Select(s => s.Id).ToList();
            Assert.Equal(6, ids[0]);
            Assert.True(window.FavouriteSpeakerButton.IsChecked);
        });
    }

    [Fact]
    public void StarringIsWrittenToSettingsUnderTheVoiceName()
    {
        var voice = WriteVoice("plain", 10, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 10);
            window.SpeakerBox.SelectedItem = window.SpeakerBox.Items.Cast<SpeakerInfo>().First(s => s.Id == 6);
            Click(window.FavouriteSpeakerButton);
        });

        var saved = AppSettings.LoadFrom(_settingsPath);
        Assert.Equal(new[] { 6 }, saved.GetFavouriteSpeakers("plain"));
    }

    [Fact]
    public void StarringTwiceLeavesTheSpeakerUnstarred()
    {
        var voice = WriteVoice("plain", 10, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 10);
            window.SpeakerBox.SelectedItem = window.SpeakerBox.Items.Cast<SpeakerInfo>().First(s => s.Id == 6);
            Click(window.FavouriteSpeakerButton);
            Click(window.FavouriteSpeakerButton);

            var ids = window.SpeakerBox.Items.Cast<SpeakerInfo>().Select(s => s.Id).ToList();
            Assert.Equal(0, ids[0]);
            Assert.False(window.FavouriteSpeakerButton.IsChecked);
        });
    }

    [Fact]
    public void TheFavouritesButtonHidesEverythingElseAndComesBack()
    {
        var voice = WriteVoice("plain", 10, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 10);
            window.SpeakerBox.SelectedItem = window.SpeakerBox.Items.Cast<SpeakerInfo>().First(s => s.Id == 6);
            Click(window.FavouriteSpeakerButton);

            Click(window.ShowFavouritesButton);
            Assert.Single(window.SpeakerBox.Items);
            Assert.Equal("Show all", window.ShowFavouritesButton.Content);

            Click(window.ShowFavouritesButton);
            Assert.Equal(10, window.SpeakerBox.Items.Count);
            Assert.Equal("Favourites", window.ShowFavouritesButton.Content);
        });
    }

    [Fact]
    public void TheFavouritesButtonSaysSoWhenNothingIsStarred()
    {
        var voice = WriteVoice("plain", 10, null);
        var window = CreateWindow();

        _wpf.Invoke(() => window.PopulateSpeakers(voice, 10));
        Click(window.ShowFavouritesButton);

        _wpf.Invoke(() =>
        {
            Assert.Contains("No starred speakers", window.StatusText.Text);
            Assert.Equal(10, window.SpeakerBox.Items.Count);
        });
    }

    [Fact]
    public void UnstarringTheLastFavouriteLeavesTheFilteredViewRatherThanAnEmptyList()
    {
        var voice = WriteVoice("plain", 10, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 10);
            window.SpeakerBox.SelectedItem = window.SpeakerBox.Items.Cast<SpeakerInfo>().First(s => s.Id == 6);
            Click(window.FavouriteSpeakerButton);
            Click(window.ShowFavouritesButton);
            Click(window.FavouriteSpeakerButton);

            Assert.Equal(10, window.SpeakerBox.Items.Count);
            Assert.Equal("Favourites", window.ShowFavouritesButton.Content);
        });
    }

    [Fact]
    public void EachVoiceRemembersItsOwnSpeaker()
    {
        var quiet = WriteVoice("quiet", 10, null);
        var loud = WriteVoice("loud", 10, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(quiet, 10);
            window.SpeakerBox.SelectedItem = window.SpeakerBox.Items.Cast<SpeakerInfo>().First(s => s.Id == 8);

            window.PopulateSpeakers(loud, 10);
            window.SpeakerBox.SelectedItem = window.SpeakerBox.Items.Cast<SpeakerInfo>().First(s => s.Id == 2);

            window.PopulateSpeakers(quiet, 10);
            Assert.Equal(8, window.SelectedSpeakerId);

            window.PopulateSpeakers(loud, 10);
            Assert.Equal(2, window.SelectedSpeakerId);
        });
    }

    [Fact]
    public void AStoredSpeakerBeyondTheVoiceRangeIsClamped()
    {
        var voice = WriteVoice("plain", 4, null);
        var window = CreateWindow(s => s.SetSpeakerId("plain", 900));

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 4);
            Assert.Equal(3, window.SelectedSpeakerId);
        });
    }

    [Fact]
    public void ASingleSpeakerVoiceHidesTheWholePickerRow()
    {
        var voice = WriteVoice("plain", 1, null);
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 1);
            Assert.Equal(Visibility.Collapsed, window.SpeakerPanel.Visibility);
        });
    }

    [Fact]
    public void ASpeakerFileBesideTheModelOverridesTheModelNames()
    {
        var voice = WriteVoice("vctk", 2, new Dictionary<int, string> { [0] = "p225", [1] = "p226" });
        File.WriteAllLines(Path.Combine(voice.Directory, SpeakerCatalog.SpeakerFileName),
            new[] { "1 = The narrator" });
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.PopulateSpeakers(voice, 2);

            var labels = window.SpeakerBox.Items.Cast<SpeakerInfo>().Select(s => s.Label).ToList();
            Assert.Equal(new[] { "0  p225", "1  The narrator" }, labels);
        });
    }

    /// <summary>Writes a model file and its piper metadata, which is all the catalogue reads.</summary>
    private VoiceDescriptor WriteVoice(string name, int speakerCount, IReadOnlyDictionary<int, string>? names)
    {
        var directory = Path.Combine(_voicesDir, name);
        Directory.CreateDirectory(directory);

        var model = Path.Combine(directory, name + ".onnx");
        File.WriteAllText(model, "not a real model");

        var map = names is null
            ? string.Empty
            : string.Join(", ", names.Select(pair => $"\"{pair.Value}\": {pair.Key}"));
        File.WriteAllText(model + ".json",
            "{ \"num_speakers\": " + speakerCount + ", \"speaker_id_map\": { " + map + " } }");

        return new VoiceDescriptor { Name = name, Directory = directory, ModelPath = model };
    }

    private void Click(ButtonBase button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

    private MainWindow CreateWindow(Action<AppSettings>? configure = null)
    {
        var window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(_settingsPath);
            settings.VoicesDirectory = _voicesDir;
            settings.UseWindowsVoices = false;
            settings.OutputDirectory = _root;
            configure?.Invoke(settings);

            return new MainWindow(settings, loadVoiceOnSelection: false);
        });

        _windows.Add(window);
        return window;
    }
}
