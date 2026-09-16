/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TtsUtil.Core.Text;

namespace TtsUtil.App;

/// <summary>The editing toolbar above the text box, and the find and replace strip under it.</summary>
public partial class MainWindow
{
    internal static readonly double[] EditorFontSizes = { 12, 14, 16, 18, 20, 24, 28 };

    private void OnUndo(object sender, RoutedEventArgs e) => InputText.Undo();

    private void OnRedo(object sender, RoutedEventArgs e) => InputText.Redo();

    private void OnCut(object sender, RoutedEventArgs e) => InputText.Cut();

    private void OnCopy(object sender, RoutedEventArgs e) => InputText.Copy();

    private void OnToggleBullets(object sender, RoutedEventArgs e) => EditBlock(TextTools.ToggleBullets);

    private void OnToggleNumbers(object sender, RoutedEventArgs e) => EditBlock(TextTools.ToggleNumbers);

    private void OnIndent(object sender, RoutedEventArgs e) => EditBlock(TextTools.Indent);

    private void OnOutdent(object sender, RoutedEventArgs e) => EditBlock(TextTools.Outdent);

    private void OnUpperCase(object sender, RoutedEventArgs e) => EditCase(LetterCase.Upper);

    private void OnLowerCase(object sender, RoutedEventArgs e) => EditCase(LetterCase.Lower);

    private void OnSentenceCase(object sender, RoutedEventArgs e) => EditCase(LetterCase.Sentence);

    private void OnTitleCase(object sender, RoutedEventArgs e) => EditCase(LetterCase.Title);

    private void OnJoinWrappedLines(object sender, RoutedEventArgs e) => EditBlock(TextTools.JoinWrappedLines);

    private void OnCollapseBlankLines(object sender, RoutedEventArgs e) => EditBlock(TextTools.CollapseBlankLines);

    private void OnTidySpacing(object sender, RoutedEventArgs e) => EditBlock(TextTools.TidySpacing);

    private void OnToggleFindCommand(object sender, ExecutedRoutedEventArgs e) => ToggleFind();

    private void OnToggleFind(object sender, RoutedEventArgs e) => ToggleFind();

    private void OnSaveScriptCommand(object sender, ExecutedRoutedEventArgs e) => SaveScriptFromText();

    /// <summary>Fills the size list and matches the box to the size the settings remembered.</summary>
    private void LoadEditorToolbar()
    {
        EditorFontSizeBox.ItemsSource = EditorFontSizes;
        EditorFontSizeBox.SelectedItem = EditorFontSizes.Contains(_settings.EditorFontSize)
            ? _settings.EditorFontSize
            : EditorFontSizes[1];

        InputText.FontSize = (double)EditorFontSizeBox.SelectedItem;
    }

    private void OnEditorFontSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InputText is null || EditorFontSizeBox.SelectedItem is not double size) return;

        InputText.FontSize = size;
        _settings.EditorFontSize = size;
    }

    private void OnWrapChanged(object sender, RoutedEventArgs e)
    {
        // The box is ticked in the markup, which fires this before the text box is built.
        if (InputText is null) return;

        var wrap = WrapTextBox.IsChecked == true;
        InputText.TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        InputText.HorizontalScrollBarVisibility = wrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
    }

    private void ToggleFind()
    {
        var showing = FindPanel.Visibility == Visibility.Visible;
        FindPanel.Visibility = showing ? Visibility.Collapsed : Visibility.Visible;

        if (!showing) FindBox.Focus();
    }

    private void OnReplaceAll(object sender, RoutedEventArgs e)
    {
        var find = FindBox.Text;
        if (find.Length == 0)
        {
            SetStatus("Type what to look for first.");
            return;
        }

        var (text, count) = TextTools.ReplaceAll(InputText.Text, find, ReplaceBox.Text,
            MatchCaseBox.IsChecked == true);

        if (count == 0)
        {
            SetStatus($"Nothing matched {find}.");
            return;
        }

        ReplaceSelection(text, 0, InputText.Text.Length);
        SetStatus(count == 1 ? $"Replaced one {find}." : $"Replaced {count} of {find}.");
    }

    private void EditCase(LetterCase letterCase) => EditBlock(block => TextTools.ChangeCase(block, letterCase));

    /// <summary>Runs a tool over the selected whole lines, or over everything when nothing
    /// is selected.</summary>
    private void EditBlock(Func<string, string> tool)
    {
        var text = InputText.Text;
        if (text.Length == 0) return;

        var (start, length) = InputText.SelectionLength > 0
            ? WholeLines(text, InputText.SelectionStart, InputText.SelectionLength)
            : (0, text.Length);

        var edited = tool(text.Substring(start, length));
        if (edited == text.Substring(start, length)) return;

        ReplaceSelection(text.Substring(0, start) + edited + text.Substring(start + length), start, edited.Length);
    }

    /// <summary>Writes through the selection, as one undo step.</summary>
    private void ReplaceSelection(string whole, int selectionStart, int selectionLength)
    {
        InputText.SelectAll();
        InputText.BeginChange();
        InputText.SelectedText = whole;
        InputText.EndChange();

        var start = Math.Min(selectionStart, whole.Length);
        InputText.Select(start, Math.Min(selectionLength, whole.Length - start));
        InputText.Focus();
    }

    /// <summary>Widens a selection to the start of its first line and the end of its last.</summary>
    private static (int Start, int Length) WholeLines(string text, int start, int length)
    {
        var from = text.LastIndexOf('\n', Math.Max(0, Math.Min(start, text.Length - 1)));
        from = from < 0 ? 0 : from + 1;

        var end = Math.Min(start + length, text.Length);
        var to = text.IndexOf('\n', Math.Max(0, Math.Min(end, text.Length - 1)));
        if (to < 0) to = text.Length;
        while (to > from && to <= text.Length && to > 0 && text[to - 1] == '\r') to--;

        return (from, to - from);
    }
}
