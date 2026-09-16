/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Text;
using CheckBox = System.Windows.Controls.CheckBox;

namespace TtsUtil.App;

/// <summary>The alias list: what the voice says in place of what was typed.</summary>
public partial class MainWindow
{
    private static readonly System.Windows.Media.Brush ClashBrush =
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFD, 0xE7, 0xE5));

    private static readonly System.Windows.Media.Brush ClashTextBrush =
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xA0, 0x21, 0x21));

    private ObservableCollection<AliasRule> _aliasRules = new();

    private AliasStore _aliasStore = AliasStore.Beside(AppSettings.SettingsPath);

    private AliasRulesetLibrary _aliasRulesets = AliasRulesetLibrary.Beside(AppSettings.SettingsPath);

    /// <summary>The list whose rules the grid is showing, empty for the rules of your own.</summary>
    private string _aliasGroup = string.Empty;

    /// <summary>The rules that never fire, against the line saying what stands in their way.</summary>
    private Dictionary<AliasRule, string> _aliasClashing = new();

    /// <summary>Where the list is kept. Tests point it somewhere temporary, which reloads it.</summary>
    internal AliasStore AliasStore
    {
        get => _aliasStore;
        set
        {
            _aliasStore = value;
            LoadAliases();
        }
    }

    /// <summary>Where saved rulesets are kept. Tests point it somewhere temporary.</summary>
    internal AliasRulesetLibrary AliasRulesets
    {
        get => _aliasRulesets;
        set
        {
            _aliasRulesets = value;
            ShowAliasRulesets();
        }
    }

    /// <summary>The rules as shown, which is also what a run uses.</summary>
    internal ObservableCollection<AliasRule> AliasRules => _aliasRules;

    /// <summary>Asks where to read an alias file from. Tests answer without a dialog.</summary>
    internal Func<string?> AliasImportPicker { get; set; } = () =>
    {
        var dialog = new OpenFileDialog { Filter = "Alias lists (*.json)|*.json|All files (*.*)|*.*" };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    /// <summary>Asks where to write an alias file. Tests answer without a dialog.</summary>
    internal Func<string?> AliasExportPicker { get; set; } = () =>
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Alias lists (*.json)|*.json",
            FileName = "aliases.json",
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    };

    /// <summary>The list a run should apply, or null when the setting is off. Everything a run
    /// uses is in the grid, including the rules a ticked list put there.</summary>
    internal AliasDictionary? ActiveAliases()
    {
        if (!_settings.UseAliases || _aliasRules.Count == 0) return null;

        return new AliasDictionary { Rules = _aliasRules.ToList() };
    }

    /// <summary>A tick box per list that ships, filled from the settings.</summary>
    private void LoadAliasPacks()
    {
        if (AliasPackPanel.Children.Count > 0) return;

        foreach (var pack in AliasPacks.All)
        {
            var box = new CheckBox
            {
                Content = pack.Name,
                Tag = pack.Id,
                Margin = new Thickness(0, 0, 14, 0),
                ToolTip = pack.Description,
                IsChecked = _settings.AliasPacks.Contains(pack.Id, StringComparer.OrdinalIgnoreCase),
            };

            box.Click += OnAliasPackChanged;
            AliasPackPanel.Children.Add(box);
        }

        ShowAliasPackCount();
    }

    /// <summary>Puts the tick boxes back to what the settings say, after a reset.</summary>
    private void ShowAliasPacks()
    {
        foreach (var box in AliasPackPanel.Children.OfType<CheckBox>())
        {
            box.IsChecked = box.Tag is string id
                && _settings.AliasPacks.Contains(id, StringComparer.OrdinalIgnoreCase);
        }

        ShowAliasPackCount();
    }

    /// <summary>Ticking a list puts its rules in the grid, where they can be read and changed.
    /// Unticking takes those same rules away again.</summary>
    private void OnAliasPackChanged(object sender, RoutedEventArgs e)
    {
        _settings.AliasPacks = AliasPackPanel.Children.OfType<CheckBox>()
            .Where(box => box.IsChecked == true)
            .Select(box => (string)box.Tag)
            .ToList();

        _settings.Save();

        if (sender is not CheckBox box || box.Tag is not string id) return;

        var message = box.IsChecked == true ? AddPackRules(id) : DropPackRules(id);
        AfterAliasChange(message);
    }

    /// <summary>A list by id, whether it ships with the program or was saved here.</summary>
    private AliasPack? FindList(string id)
    {
        if (AliasPacks.Find(id) is AliasPack shipped) return shipped;
        if (AliasRulesetLibrary.TitleOf(id) is not string title) return null;

        var saved = _aliasRulesets.Load(title);
        return saved is null ? null : new AliasPack { Id = id, Name = title, Rules = saved.Rules };
    }

    /// <summary>What to call a list on a tab and in a message.</summary>
    private string ListName(string id) => FindList(id)?.Name ?? AliasRulesetLibrary.TitleOf(id) ?? id;

    /// <summary>Adds a list's rules under the ones already there, skipping words already covered.</summary>
    private string AddPackRules(string id)
    {
        var pack = FindList(id);
        if (pack is null) return "That list is no longer here.";

        var known = new HashSet<string>(
            _aliasRules.Select(rule => rule.Match), StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var rule in pack.Copies())
        {
            if (!known.Add(rule.Match)) continue;

            _aliasRules.Add(rule);
            added++;
        }

        return added == 0
            ? $"Every rule in {pack.Name} was already here."
            : $"Added {added} rules from {pack.Name}. They can be read and changed above.";
    }

    /// <summary>Unticking takes back what the list gave and never what you changed: an edited
    /// rule stays behind as one of your own.</summary>
    private string DropPackRules(string id)
    {
        var theirs = new Dictionary<string, AliasRule>(StringComparer.Ordinal);

        foreach (var rule in FindList(id)?.Rules ?? Array.Empty<AliasRule>()) theirs[rule.Match] = rule;

        var going = new List<AliasRule>();
        var mine = 0;

        foreach (var rule in _aliasRules.Where(rule =>
            string.Equals(rule.Source, id, StringComparison.OrdinalIgnoreCase)))
        {
            if (theirs.TryGetValue(rule.Match, out var shipped) && SameRule(rule, shipped))
            {
                going.Add(rule);
                continue;
            }

            rule.Source = string.Empty;
            mine++;
        }

        foreach (var rule in going) _aliasRules.Remove(rule);
        if (mine > 0) AliasList.Items.Refresh();

        var kept = mine == 0 ? string.Empty : $", keeping the {mine} you changed";

        return going.Count == 0
            ? "Nothing was taken away: those rules are your own now."
            : $"Took {going.Count} rules from {ListName(id)} back out{kept}.";
    }

    private static bool SameRule(AliasRule rule, AliasRule other) =>
        rule.SayAs == other.SayAs
        && rule.WholeWord == other.WholeWord
        && rule.MatchCase == other.MatchCase
        && rule.Enabled == other.Enabled;

    private void ShowAliasPackCount()
    {
        var rules = _aliasRules.Count(rule => rule.Source.Length > 0);

        AliasPackText.Text = rules == 0
            ? "None of their rules is in the list above."
            : $"{rules} of the rules above came from a ticked list.";
    }

    /// <summary>A tick box per ruleset saved here, beside the lists that ship.</summary>
    private void ShowAliasRulesets()
    {
        if (AliasRulesetPanel is null) return;

        AliasRulesetPanel.Children.Clear();
        var titles = _aliasRulesets.Titles();

        foreach (var title in titles)
        {
            var id = AliasRulesetLibrary.IdFor(title);

            var box = new CheckBox
            {
                Content = title,
                Tag = id,
                Margin = new Thickness(0, 0, 14, 0),
                ToolTip = "Put this ruleset's rules in the list above",
                IsChecked = _settings.AliasPacks.Contains(id, StringComparer.OrdinalIgnoreCase),
            };

            box.Click += OnAliasPackChanged;
            AliasRulesetPanel.Children.Add(box);
        }

        AliasRulesetText.Text = titles.Count == 0
            ? "Nothing saved yet. Name the rules on show above to keep them as a ruleset of your own."
            : "Tick one to put its rules in the list above, untick it to take them back out.";
    }

    /// <summary>Keeps the rules on show under a name, so they can be put back later.</summary>
    private void OnSaveAliasRuleset(object sender, RoutedEventArgs e)
    {
        if (!ClaimClick()) return;

        var title = AliasRulesetTitleBox.Text.Trim();

        if (title.Length == 0)
        {
            SetStatus("Type a name for the ruleset first.");
            return;
        }

        var rules = AliasList.Items.OfType<AliasRule>().ToList();

        if (rules.Count == 0)
        {
            SetStatus("There are no rules on this tab to save.");
            return;
        }

        if (_aliasRulesets.Exists(title)
            && !Confirm($"A ruleset called {title} is already saved. Replace it?", "Save ruleset"))
        {
            return;
        }

        try
        {
            if (!_aliasRulesets.Save(title, rules))
            {
                SetStatus("There is nothing in that name a file can be called.");
                return;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus($"Could not save the ruleset: {ex.Message}");
            return;
        }

        ShowAliasRulesets();
        SetStatus($"Saved {rules.Count} rules as {title}.");
    }

    /// <summary>Forgets a saved ruleset. The rules it put in the list are taken out with it.</summary>
    private void OnDeleteAliasRuleset(object sender, RoutedEventArgs e)
    {
        if (!ClaimClick()) return;

        var title = AliasRulesetTitleBox.Text.Trim();

        if (title.Length == 0 || !_aliasRulesets.Exists(title))
        {
            SetStatus("Type the name of a saved ruleset first.");
            return;
        }

        if (!Confirm($"Delete the saved ruleset {title}?", "Delete ruleset")) return;

        var id = AliasRulesetLibrary.IdFor(title);
        var message = DropPackRules(id);

        _settings.AliasPacks = _settings.AliasPacks
            .Where(ticked => !string.Equals(ticked, id, StringComparison.OrdinalIgnoreCase)).ToList();
        _settings.Save();

        _aliasRulesets.Delete(title);
        ShowAliasRulesets();
        AfterAliasChange($"Deleted the ruleset {title}. {message}");
    }

    /// <summary>Empties the alias list, writing what was there to a .bak file first.</summary>
    internal void ResetAliases()
    {
        if (_aliasRules.Count == 0)
        {
            SetStatus("The alias list is already empty.");
            return;
        }

        var question = $"Clear all {_aliasRules.Count} alias rules? A copy is kept beside the list "
            + "as aliases.json.bak.";

        if (!Confirm(question, "Reset aliases")) return;

        string? backup;

        try
        {
            backup = _aliasStore.Backup();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus($"Nothing was cleared: a copy could not be kept. {ex.Message}");
            return;
        }

        _aliasRules.Clear();

        _settings.AliasPacks = new List<string>();
        _settings.Save();
        ShowAliasPacks();
        ShowAliasRulesets();

        AfterAliasChange(backup is null
            ? "The alias list is empty."
            : $"The alias list is empty. The old one is in {Path.GetFileName(backup)}.");
    }

    private void LoadAliases()
    {
        var stored = _aliasStore.Load();
        _aliasRules = new ObservableCollection<AliasRule>(stored.Rules);
        AliasList.ItemsSource = _aliasRules;
        CollectionViewSource.GetDefaultView(_aliasRules).Filter = InGroup;
        UseAliasesBox.IsChecked = _settings.UseAliases;
        LoadAliasPacks();
        ShowAliasRulesets();
        ShowAliasGroups();
        ShowAliasCount();
        ShowAliasClashes();
    }

    private void SaveAliases()
    {
        try
        {
            _aliasStore.Save(new AliasDictionary { Rules = _aliasRules.ToList() });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus($"Could not save the alias list: {ex.Message}");
        }
    }

    private void OnAddAlias(object sender, RoutedEventArgs e)
    {
        var rule = new AliasRule { Match = AliasMatchBox.Text.Trim(), SayAs = AliasSayAsBox.Text };

        if (rule.Match.Length == 0)
        {
            SetStatus("Type the word to look for first.");
            return;
        }

        ShowAliasGroup(string.Empty);
        _aliasRules.Add(rule);
        AliasMatchBox.Clear();
        AliasSayAsBox.Clear();
        AliasMatchBox.Focus();
        AfterAliasChange($"Added {rule.Match}.");
    }

    private void OnRemoveAlias(object sender, RoutedEventArgs e)
    {
        if (AliasList.SelectedItem is not AliasRule rule)
        {
            SetStatus("Pick a rule to remove first.");
            return;
        }

        _aliasRules.Remove(rule);
        AfterAliasChange($"Removed {rule.Match}.");
    }

    private void OnMoveAliasUp(object sender, RoutedEventArgs e) => MoveAlias(-1);

    private void OnMoveAliasDown(object sender, RoutedEventArgs e) => MoveAlias(1);

    /// <summary>Order decides which rule sees the text first, so it is worth being able to change.</summary>
    private void MoveAlias(int by)
    {
        if (AliasList.SelectedItem is not AliasRule rule) return;

        var shown = AliasList.Items.OfType<AliasRule>().ToList();
        var at = shown.IndexOf(rule);
        var next = at + by;

        if (at < 0 || next < 0 || next >= shown.Count) return;

        _aliasRules.Move(_aliasRules.IndexOf(rule), _aliasRules.IndexOf(shown[next]));
        AliasList.SelectedItem = rule;
        AfterAliasChange(null);
    }

    private void OnAliasRuleEdited(object sender, DataGridCellEditEndingEventArgs e) =>
        Dispatcher.BeginInvoke(() => AfterAliasChange(null));

    private void OnUseAliasesChanged(object sender, RoutedEventArgs e)
    {
        if (AliasList is null) return;

        _settings.UseAliases = UseAliasesBox.IsChecked == true;
        _settings.Save();
        SettingsUseAliasesBox.IsChecked = UseAliasesBox.IsChecked;
        ShowAliasPreview();
    }

    /// <summary>Runs the list over the try it box, so the effect of a rule is visible.</summary>
    private void ShowAliasPreview()
    {
        if (AliasTryBox is null || AliasResultText is null) return;

        var text = AliasTryBox.Text;

        if (text.Length == 0)
        {
            AliasResultText.Text = string.Empty;
            return;
        }

        AliasResultText.Text = ActiveAliases() is AliasDictionary aliases ? aliases.Apply(text) : text;
    }

    private void OnAliasTryChanged(object sender, TextChangedEventArgs e) => ShowAliasPreview();

    private void OnImportAliases(object sender, RoutedEventArgs e)
    {
        var path = AliasImportPicker();
        if (path is null) return;

        AliasDictionary? read;

        try
        {
            read = AliasDictionary.FromJson(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus($"Could not open {Path.GetFileName(path)}: {ex.Message}");
            return;
        }

        if (read is null)
        {
            SetStatus($"{Path.GetFileName(path)} is not an alias list this version can read.");
            return;
        }

        var mine = new AliasDictionary { Rules = _aliasRules.ToList() };
        var added = mine.Merge(read);

        foreach (var rule in mine.Rules.Skip(_aliasRules.Count)) _aliasRules.Add(rule);

        var named = string.IsNullOrWhiteSpace(read.Name) ? Path.GetFileName(path) : read.Name;
        AfterAliasChange(added == 0
            ? $"Every rule in {named} was already here."
            : $"Added {added} rule{(added == 1 ? string.Empty : "s")} from {named}.");
    }

    private void OnExportAliases(object sender, RoutedEventArgs e)
    {
        if (_aliasRules.Count == 0)
        {
            SetStatus("There are no aliases to share yet.");
            return;
        }

        var path = AliasExportPicker();
        if (path is null) return;

        var list = new AliasDictionary
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Rules = _aliasRules.ToList(),
        };

        try
        {
            File.WriteAllText(path, list.ToJson());
            SetStatus($"Wrote {_aliasRules.Count} rules to {Path.GetFileName(path)}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus($"Could not write {Path.GetFileName(path)}: {ex.Message}");
        }
    }

    private void AfterAliasChange(string? message)
    {
        SaveAliases();
        ShowAliasGroups();
        ShowAliasCount();
        ShowAliasPackCount();
        ShowAliasClashes();
        ShowAliasPreview();

        if (message is not null) SetStatus(message);
    }

    private void ShowAliasCount()
    {
        var count = _aliasRules.Count;
        AliasCountText.Text = count == 1 ? "1 alias" : $"{count} aliases";
    }

    /// <summary>One tab for the rules of your own and one for each ticked list, so a list can be
    /// read on its own rather than scrolled past.</summary>
    private void ShowAliasGroups()
    {
        if (AliasGroupTabs is null) return;

        var wanted = new List<(string Id, string Name)> { (string.Empty, "My rules") };

        foreach (var id in _aliasRules.Select(rule => rule.Source)
            .Where(source => source.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            wanted.Add((id, ListName(id)));
        }

        var shown = AliasGroupTabs.Items.OfType<TabItem>().Select(tab => (string)tab.Tag).ToList();
        if (shown.SequenceEqual(wanted.Select(group => group.Id))) return;

        AliasGroupTabs.Items.Clear();

        foreach (var (id, name) in wanted)
        {
            AliasGroupTabs.Items.Add(new TabItem { Header = name, Tag = id });
        }

        ShowAliasGroup(wanted.Any(group => group.Id == _aliasGroup) ? _aliasGroup : string.Empty);
    }

    /// <summary>Puts the grid on one group, picking its tab as well.</summary>
    private void ShowAliasGroup(string id)
    {
        _aliasGroup = id;

        AliasGroupTabs.SelectedItem = AliasGroupTabs.Items.OfType<TabItem>()
            .FirstOrDefault(tab => (string)tab.Tag == id);

        CollectionViewSource.GetDefaultView(_aliasRules).Refresh();
    }

    private void OnAliasGroupChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AliasGroupTabs.SelectedItem is not TabItem tab || (string)tab.Tag == _aliasGroup) return;

        ShowAliasGroup((string)tab.Tag);
    }

    private bool InGroup(object item) =>
        item is AliasRule rule && string.Equals(rule.Source, _aliasGroup, StringComparison.OrdinalIgnoreCase);

    /// <summary>Names the rules an earlier rule already swallows, which is how a list ends up
    /// quietly doing nothing.</summary>
    private void ShowAliasClashes()
    {
        if (AliasClashText is null) return;

        var found = AliasConflicts.Find(_aliasRules);
        _aliasClashing = found.ToDictionary(conflict => conflict.Rule, conflict => conflict.Describe());

        AliasClashText.Text = found.Count == 0 ? string.Empty : AliasConflicts.Summarize(_aliasRules);
        AliasClashText.Visibility = found.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        foreach (var rule in _aliasRules)
        {
            if (AliasList.ItemContainerGenerator.ContainerFromItem(rule) is DataGridRow row) PaintAliasRow(row);
        }
    }

    private void OnAliasRowLoaded(object sender, DataGridRowEventArgs e) => PaintAliasRow(e.Row);

    /// <summary>A rule that never fires is shown in red, with the rule above it named.</summary>
    private void PaintAliasRow(DataGridRow row)
    {
        if (row.Item is AliasRule rule && _aliasClashing.TryGetValue(rule, out var why))
        {
            row.Background = ClashBrush;
            row.Foreground = ClashTextBrush;
            row.ToolTip = why;
            return;
        }

        row.ClearValue(BackgroundProperty);
        row.ClearValue(ForegroundProperty);
        row.ClearValue(ToolTipProperty);
    }
}
