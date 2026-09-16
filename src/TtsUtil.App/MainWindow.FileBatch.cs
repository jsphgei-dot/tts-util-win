/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.IO;
using System.Windows;
using TtsUtil.Core.Settings;

namespace TtsUtil.App;

/// <summary>Several files taken in at one go, each one becoming its own tab or its own saved
/// script, named after the file it came from.</summary>
public partial class MainWindow
{
    /// <summary>Asks which files to take in. Tests replace it so no dialog opens.</summary>
    internal Func<IReadOnlyList<string>> BatchFilePicker { get; set; } = () => Array.Empty<string>();

    /// <summary>Set when Stop ended the last extraction, so the rest of a batch is dropped.</summary>
    private bool _batchStopped;

    /// <summary>What the File tab accepts and what it does with it, shown by the question mark
    /// beside Browse.</summary>
    internal const string FileHelpNote =
        "Plain text files (.txt, .md, .csv, .log) and PDF documents (.pdf) can be read in. " +
        "Any other file can still be chosen, and is treated as plain text.\n\n" +
        "A PDF has its words pulled out page by page, so a long one takes a moment and Stop ends " +
        "it early. Scanned pages that hold pictures of text rather than text itself come back " +
        "empty, as nothing in them can be read.\n\n" +
        "One file at a time: Browse, then Read to hear it, or Import to bring it into the open " +
        "document.\n\n" +
        "Several at once: Open files in tabs gives each file its own tab, and Save files as " +
        "scripts puts each one in the script library. Both name what they make after the file it " +
        "came from.";

    private void OnFileHelp(object sender, RoutedEventArgs e) =>
        MessageBox.Show(this, FileHelpNote, "Taking files in", MessageBoxButton.OK,
            MessageBoxImage.Information);

    private void OnOpenManyInTabs(object sender, RoutedEventArgs e) =>
        PendingWork = TakeInFilesAsync(openTabs: true, saveScripts: AlsoSaveScriptsBox.IsChecked == true);

    private void OnSaveManyAsScripts(object sender, RoutedEventArgs e) =>
        PendingWork = TakeInFilesAsync(openTabs: false, saveScripts: true);

    /// <summary>Reads the chosen files one after another, so a long PDF never blocks the rest.
    /// A file can become a tab, a script, or both.</summary>
    internal async Task TakeInFilesAsync(bool openTabs, bool saveScripts)
    {
        var paths = BatchFilePicker();
        if (paths.Count == 0) return;

        var taken = new List<string>();
        var skipped = new List<string>();
        TextDocument? first = null;
        _batchStopped = false;

        foreach (var path in paths)
        {
            var text = await ReadFileTextAsync(path);
            if (_batchStopped) break;

            var name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(text) || (saveScripts && !SaveAsScript(name, text)))
            {
                skipped.Add(Path.GetFileName(path));
                continue;
            }

            if (openTabs)
            {
                var opened = NewDocument(name, text, saveScripts ? ScriptLibrary.ToFileName(name) : null);
                first ??= opened;
            }

            taken.Add(name);
        }

        if (saveScripts) RefreshScripts();

        if (first is not null)
        {
            // The first one taken in is the one waiting when the Text tab is next looked at.
            DocumentTabs.SelectedItem = first.Tab;
            MarkNewTabs(taken.Count);
        }

        ReportBatch(taken, skipped, openTabs, saveScripts);
    }

    private bool SaveAsScript(string name, string text)
    {
        var title = ScriptLibrary.ToFileName(name);
        if (title is null) return false;

        try
        {
            Scripts.Save(title, text);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void ReportBatch(IReadOnlyList<string> taken, IReadOnlyList<string> skipped,
        bool openTabs, bool saveScripts)
    {
        var where = openTabs && saveScripts ? "opened in tab(s) and saved as script(s)"
            : saveScripts ? "saved as script(s)"
            : "opened in tab(s)";
        var note = taken.Count == 0
            ? $"Nothing was {where}."
            : $"{taken.Count} file(s) {where}: {string.Join(", ", taken)}.";

        if (skipped.Count > 0) note += $" Nothing could be read from: {string.Join(", ", skipped)}.";
        if (_batchStopped) note += " The rest were left alone.";

        FileInfoText.Text = note;
        SetStatus(note);
    }

    /// <summary>The words inside a PDF or a text file, or null when nothing could be read.</summary>
    private async Task<string?> ReadFileTextAsync(string path)
    {
        if (!File.Exists(path)) return null;
        if (IsPdf(path)) return await ExtractPdfAsync(path);

        try
        {
            using var reader = OpenTextFile(path);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private IReadOnlyList<string> AskForFilesToTakeIn()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose PDFs or text files",
            Multiselect = true,
            Filter = "Readable files (*.txt;*.md;*.csv;*.log;*.pdf)|*.txt;*.md;*.csv;*.log;*.pdf|" +
                     "PDF documents (*.pdf)|*.pdf|Text files (*.txt;*.md;*.csv;*.log)|*.txt;*.md;*.csv;*.log|" +
                     "All files (*.*)|*.*",
        };

        return dialog.ShowDialog(this) == true ? dialog.FileNames : Array.Empty<string>();
    }
}
