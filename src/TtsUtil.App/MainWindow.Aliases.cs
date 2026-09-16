/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Text;

namespace TtsUtil.App;

/// <summary>The alias list: what the voice says in place of what was typed.</summary>
public partial class MainWindow
{
    private ObservableCollection<AliasRule> _aliasRules = new();

    private AliasStore _aliasStore = AliasStore.Beside(AppSettings.SettingsPath);

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

    /// <summary>The list a run should apply, or null when the setting is off.</summary>
    internal AliasDictionary? ActiveAliases() =>
        _settings.UseAliases && _aliasRules.Count > 0
            ? new AliasDictionary { Rules = _aliasRules.ToList() }
            : null;

    private void LoadAliases()
    {
        var stored = _aliasStore.Load();
        _aliasRules = new ObservableCollection<AliasRule>(stored.Rules);
        AliasList.ItemsSource = _aliasRules;
        UseAliasesBox.IsChecked = _settings.UseAliases;
        ShowAliasCount();
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
        var from = AliasList.SelectedIndex;
        var to = from + by;

        if (from < 0 || to < 0 || to >= _aliasRules.Count) return;

        _aliasRules.Move(from, to);
        AliasList.SelectedIndex = to;
        AfterAliasChange(null);
    }

    private void OnAliasRuleEdited(object sender, DataGridCellEditEndingEventArgs e) =>
        Dispatcher.BeginInvoke(() => AfterAliasChange(null));

    private void OnUseAliasesChanged(object sender, RoutedEventArgs e)
    {
        if (AliasList is null) return;

        _settings.UseAliases = UseAliasesBox.IsChecked == true;
        _settings.Save();
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

        if (_settings.UseAliases)
        {
            var aliases = new AliasDictionary { Rules = _aliasRules.ToList() };
            AliasResultText.Text = aliases.Apply(text);
            return;
        }

        AliasResultText.Text = text;
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
        ShowAliasCount();
        ShowAliasPreview();

        if (message is not null) SetStatus(message);
    }

    private void ShowAliasCount()
    {
        var count = _aliasRules.Count;
        AliasCountText.Text = count == 1 ? "1 alias" : $"{count} aliases";
    }
}
