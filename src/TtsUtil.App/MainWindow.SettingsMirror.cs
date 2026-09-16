/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Controls;
using TtsUtil.Core.Settings;

namespace TtsUtil.App;

/// <summary>Settings that live on their own tab as well, so everything can be found in one place.
/// The pair stay in step, and either one writes the setting.</summary>
public partial class MainWindow
{
    private static readonly RepeatMode[] RepeatModes = { RepeatMode.Off, RepeatMode.One, RepeatMode.All };

    /// <summary>Fills the copies on the Settings tab from the controls that own each setting.</summary>
    private void LoadSettingsMirror()
    {
        if (SettingsRepeatBox.Items.Count == 0)
        {
            SettingsRepeatBox.Items.Add("Off, stop at the end");
            SettingsRepeatBox.Items.Add("One, read the same text again");
            SettingsRepeatBox.Items.Add("All, move through the queue");
        }

        if (SettingsFontSizeBox.ItemsSource is null) SettingsFontSizeBox.ItemsSource = EditorFontSizes;

        LoadWriteNoticeChoices();

        SettingsReadAsYouTypeBox.IsChecked = ReadAsYouTypeBox.IsChecked;
        SettingsPauseWhenUnfocusedBox.IsChecked = PauseWhenUnfocusedBox.IsChecked;
        SettingsUseAliasesBox.IsChecked = UseAliasesBox.IsChecked;
        SettingsSpeedBox.Text = $"{_settings.Speed:0.00}";
        SettingsFontSizeBox.SelectedItem = EditorFontSizeBox.SelectedItem;
        SettingsRepeatBox.SelectedIndex = Math.Max(0, Array.IndexOf(RepeatModes, _settings.Repeat));
    }

    private void OnSettingsReadAsYouTypeChanged(object sender, RoutedEventArgs e)
    {
        if (_initialising) return;
        ReadAsYouTypeBox.IsChecked = SettingsReadAsYouTypeBox.IsChecked;
    }

    private void OnSettingsPauseWhenUnfocusedChanged(object sender, RoutedEventArgs e)
    {
        if (_initialising) return;
        PauseWhenUnfocusedBox.IsChecked = SettingsPauseWhenUnfocusedBox.IsChecked;
    }

    private void OnSettingsUseAliasesChanged(object sender, RoutedEventArgs e)
    {
        if (_initialising) return;
        UseAliasesBox.IsChecked = SettingsUseAliasesBox.IsChecked;
    }

    private void OnSettingsFontSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialising || SettingsFontSizeBox.SelectedItem is not double size) return;
        EditorFontSizeBox.SelectedItem = size;
    }

    private void OnSettingsRepeatChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialising || SettingsRepeatBox.SelectedIndex < 0) return;

        _settings.Repeat = RepeatModes[SettingsRepeatBox.SelectedIndex];
        _settings.Save();
        ShowRepeatMode();
    }

    /// <summary>Puts every setting back to a first run, leaving scripts, aliases and audio alone.</summary>
    private void OnResetSettings(object sender, RoutedEventArgs e)
    {
        var question = "Put every setting back to how it started? Scripts, aliases and saved audio are left alone.";
        if (!Confirm(question, "Reset settings")) return;

        _settings.ResetToDefaults();

        LoadSettingsIntoUi();
        LoadEditorToolbar();
        UseAliasesBox.IsChecked = _settings.UseAliases;
        ShowAliasPacks();
        ShowRepeatMode();
        LoadSettingsMirror();
        RefreshVoices();

        SetStatus("Settings are back to their defaults.");
    }
}
