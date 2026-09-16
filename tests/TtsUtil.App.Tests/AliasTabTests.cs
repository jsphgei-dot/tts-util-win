using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class AliasTabTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _aliasPath;
    private readonly MainWindow _window;

    public AliasTabTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinAliases", Guid.NewGuid().ToString("N"));
        _aliasPath = Path.Combine(_root, "aliases.json");
        Directory.CreateDirectory(Path.Combine(_root, "voices"));

        _window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = Path.Combine(_root, "voices");
            settings.UseWindowsVoices = false;
            settings.OutputDirectory = _root;

            return new MainWindow(settings, loadVoiceOnSelection: false)
            {
                AliasStore = new AliasStore(_aliasPath),
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

    /// <summary>A list that ships is off until it is ticked, and then it reads alongside the
    /// rules of your own.</summary>
    [Fact]
    public void AListThatShipsOnlyAppliesOnceItIsTicked()
    {
        _wpf.Invoke(() =>
        {
            Assert.Empty(_window.Settings.AliasPacks);
            Assert.Null(_window.ActiveAliases());

            var chemistry = _window.AliasPackPanel.Children
                .OfType<System.Windows.Controls.CheckBox>()
                .First(box => (string)box.Tag == "chemistry");

            chemistry.IsChecked = true;
            chemistry.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            Assert.Equal(new[] { "chemistry" }, _window.Settings.AliasPacks);
            Assert.Equal("potassium", _window.ActiveAliases()!.Apply("K"));
        });
    }

    [Fact]
    public void AnAddedAliasIsListedAndWrittenToDisk()
    {
        Add("SQL", "sequel");

        _wpf.Invoke(() => Assert.Equal("sequel", Assert.Single(_window.AliasRules).SayAs));
        Assert.Contains("sequel", File.ReadAllText(_aliasPath));
    }

    [Fact]
    public void AnAliasWithNothingToMatchIsRefused()
    {
        _wpf.Invoke(() => _window.AliasSayAsBox.Text = "sequel");

        Click(_window.AddAliasButton);

        _wpf.Invoke(() =>
        {
            Assert.Empty(_window.AliasRules);
            Assert.Equal("Type the word to look for first.", _window.StatusHistory[^1]);
        });
    }

    [Fact]
    public void TheTryItBoxShowsWhatTheVoiceWouldBeGiven()
    {
        Add("SQL", "sequel");

        _wpf.Invoke(() => _window.AliasTryBox.Text = "the SQL server");

        _wpf.Invoke(() => Assert.Equal("the sequel server", _window.AliasResultText.Text));
    }

    [Fact]
    public void TurningAliasesOffLeavesThePreviewAlone()
    {
        Add("SQL", "sequel");

        _wpf.Invoke(() =>
        {
            _window.AliasTryBox.Text = "the SQL server";
            _window.UseAliasesBox.IsChecked = false;
        });

        Click(_window.UseAliasesBox);

        _wpf.Invoke(() =>
        {
            Assert.Equal("the SQL server", _window.AliasResultText.Text);
            Assert.False(_window.Settings.UseAliases);
        });
    }

    [Fact]
    public void MovingARuleUpChangesWhichOneSeesTheTextFirst()
    {
        Add("a", "b");
        Add("b", "c");

        _wpf.Invoke(() => _window.AliasList.SelectedIndex = 1);
        Click(_window.MoveAliasUpButton);

        _wpf.Invoke(() => Assert.Equal(new[] { "b", "a" }, _window.AliasRules.Select(rule => rule.Match)));
    }

    [Fact]
    public void ImportingAddsTheRulesThatAreNotAlreadyHere()
    {
        Add("SQL", "sequel");

        var shared = Path.Combine(_root, "shared.json");
        File.WriteAllText(shared, new AliasDictionary
        {
            Name = "Work words",
            Rules = { new AliasRule { Match = "SQL", SayAs = "ess queue ell" }, new AliasRule { Match = "GIF", SayAs = "jif" } },
        }.ToJson());

        _wpf.Invoke(() => _window.AliasImportPicker = () => shared);
        Click(_window.ImportAliasesButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal(new[] { "sequel", "jif" }, _window.AliasRules.Select(rule => rule.SayAs));
            Assert.Equal("Added 1 rule from Work words.", _window.StatusHistory[^1]);
        });
    }

    [Fact]
    public void ImportingSomethingThatIsNotAnAliasListSaysSo()
    {
        var junk = Path.Combine(_root, "junk.json");
        File.WriteAllText(junk, "this is not json");

        _wpf.Invoke(() => _window.AliasImportPicker = () => junk);
        Click(_window.ImportAliasesButton);

        _wpf.Invoke(() =>
        {
            Assert.Empty(_window.AliasRules);
            Assert.Contains("is not an alias list", _window.StatusHistory[^1]);
        });
    }

    [Fact]
    public void ExportingWritesAFileThatCanBeReadBack()
    {
        Add("SQL", "sequel");

        var target = Path.Combine(_root, "mine.json");
        _wpf.Invoke(() => _window.AliasExportPicker = () => target);
        Click(_window.ExportAliasesButton);

        var read = AliasDictionary.FromJson(File.ReadAllText(target));
        Assert.Equal("sequel", Assert.Single(read!.Rules).SayAs);
    }

    [Fact]
    public void AStoredListIsThereOnTheNextStart()
    {
        Add("SQL", "sequel");

        var reopened = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = Path.Combine(_root, "voices");
            settings.UseWindowsVoices = false;

            return new MainWindow(settings, loadVoiceOnSelection: false) { AliasStore = new AliasStore(_aliasPath) };
        });

        _wpf.Invoke(() =>
        {
            Assert.Equal("SQL", Assert.Single(reopened.AliasRules).Match);
            reopened.Close();
        });
    }

    private void Add(string match, string sayAs)
    {
        _wpf.Invoke(() =>
        {
            _window.AliasMatchBox.Text = match;
            _window.AliasSayAsBox.Text = sayAs;
        });

        Click(_window.AddAliasButton);
    }

    private void Click(ButtonBase button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));
}
