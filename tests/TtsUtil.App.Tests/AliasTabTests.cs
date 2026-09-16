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
                AliasRulesets = new AliasRulesetLibrary(Path.Combine(_root, "rulesets")),
                Confirmer = (_, _) => true,
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

    /// <summary>Ticking a list puts its rules in the grid, where they can be read and edited,
    /// and unticking takes those same rules back out.</summary>
    [Fact]
    public void TickingAListPutsItsRulesInTheGrid()
    {
        _wpf.Invoke(() =>
        {
            Assert.Empty(_window.Settings.AliasPacks);
            Assert.Null(_window.ActiveAliases());

            Tick("chemistry", true);

            Assert.Equal(new[] { "chemistry" }, _window.Settings.AliasPacks);
            Assert.Contains(_window.AliasRules, rule => rule.Match == "K" && rule.Source == "chemistry");
            Assert.Equal("potassium", _window.ActiveAliases()!.Apply("K"));

            Tick("chemistry", false);

            Assert.Empty(_window.AliasRules);
            Assert.Null(_window.ActiveAliases());
        });
    }

    /// <summary>A rule you changed stays behind when the list that brought it is unticked.</summary>
    [Fact]
    public void AnEditedRuleSurvivesUnticking()
    {
        _wpf.Invoke(() =>
        {
            Tick("chemistry", true);
            _window.AliasRules.First(rule => rule.Match == "K").SayAs = "the potassium one";
            Tick("chemistry", false);

            var kept = Assert.Single(_window.AliasRules);
            Assert.Equal("K", kept.Match);
            Assert.Equal(string.Empty, kept.Source);
        });
    }

    /// <summary>Rules saved under a name come back as a list of their own, ticked like the
    /// ones that ship.</summary>
    [Fact]
    public void ASavedRulesetIsTickedBackIn()
    {
        Add("SQL", "sequel");

        _wpf.Invoke(() =>
        {
            _window.AliasRulesetTitleBox.Text = "Work words";
            _window.SaveAliasRulesetButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            _window.AliasRules.Clear();
            TickRuleset("Work words", true);

            var rule = Assert.Single(_window.AliasRules);
            Assert.Equal("sequel", rule.SayAs);
            Assert.Equal(new[] { "My rules", "Work words" }, Tabs());
        });
    }

    /// <summary>Clearing the list leaves the old one beside it, rather than throwing it away.</summary>
    [Fact]
    public void ResettingTheAliasesKeepsTheOldListAsABak()
    {
        Add("SQL", "sequel");

        _wpf.Invoke(() => _window.ResetAliasesButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

        _wpf.Invoke(() => Assert.Empty(_window.AliasRules));
        Assert.Contains("sequel", File.ReadAllText(_aliasPath + ".bak"));
    }

    private void TickRuleset(string title, bool on)
    {
        var box = _window.AliasRulesetPanel.Children
            .OfType<System.Windows.Controls.CheckBox>()
            .First(item => (string)item.Content == title);

        box.IsChecked = on;
        box.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    }

    /// <summary>A rule an earlier one already swallows is named, rather than quietly doing
    /// nothing.</summary>
    [Fact]
    public void ARuleThatNeverFiresIsNamedOnTheTab()
    {
        Add("GNU", "gnew");
        Add("GNU", "guh noo");

        _wpf.Invoke(() =>
        {
            Assert.Equal(Visibility.Visible, _window.AliasClashText.Visibility);
            Assert.Contains("GNU", _window.AliasClashText.Text);
        });
    }

    /// <summary>Each ticked list gets its own tab, and the grid shows that list on its own.</summary>
    [Fact]
    public void ATickedListGetsItsOwnTab()
    {
        Add("SQL", "sequel");

        _wpf.Invoke(() =>
        {
            Assert.Equal(new[] { "My rules" }, Tabs());

            Tick("chemistry", true);
            Assert.Equal(new[] { "My rules", "Chemistry" }, Tabs());
            Assert.Equal("SQL", Assert.Single(_window.AliasList.Items.OfType<AliasRule>()).Match);

            _window.AliasGroupTabs.SelectedIndex = 1;

            Assert.All(_window.AliasList.Items.OfType<AliasRule>(),
                rule => Assert.Equal("chemistry", rule.Source));

            Tick("chemistry", false);
            Assert.Equal(new[] { "My rules" }, Tabs());
        });
    }

    private string[] Tabs() => _window.AliasGroupTabs.Items
        .OfType<System.Windows.Controls.TabItem>()
        .Select(tab => (string)tab.Header)
        .ToArray();

    private void Tick(string id, bool on)
    {
        var box = _window.AliasPackPanel.Children
            .OfType<System.Windows.Controls.CheckBox>()
            .First(item => (string)item.Tag == id);

        box.IsChecked = on;
        box.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
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
