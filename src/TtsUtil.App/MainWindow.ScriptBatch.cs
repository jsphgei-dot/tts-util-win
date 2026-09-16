/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.IO;
using System.Windows;
using TtsUtil.Core.Audio;
using TtsUtil.Core.Settings;

namespace TtsUtil.App;

/// <summary>Writing several saved scripts to audio files in one go.</summary>
public partial class MainWindow
{
    /// <summary>Picks the folder a batch is written into. Tests answer without a dialog.</summary>
    internal Func<string?> BatchFolderPicker { get; set; }

    /// <summary>The scripts that are ticked, in the order the list shows them.</summary>
    internal IReadOnlyList<SavedScript> ChosenScripts() =>
        ScriptList.Items.Cast<ScriptRow>().Where(row => row.Chosen).Select(row => row.Script).ToList();

    private void OnTickEveryScript(object sender, RoutedEventArgs e) => TickScripts(true);

    private void OnClearScriptTicks(object sender, RoutedEventArgs e) => TickScripts(false);

    private void TickScripts(bool chosen)
    {
        foreach (var row in ScriptList.Items.Cast<ScriptRow>()) row.Chosen = chosen;
    }

    private void OnConvertScripts(object sender, RoutedEventArgs e)
    {
        var chosen = ChosenScripts();
        if (chosen.Count == 0)
        {
            SetStatus("Tick the scripts you want to convert first.");
            return;
        }

        var folder = BatchFolderPicker();
        if (folder is null) return;

        PendingWork = SaveThenResumeAsync(() => CurrentRun = ConvertScriptsAsync(chosen, folder));
    }

    /// <summary>Writes one file per script, one at a time, and stops when the user stops a file.</summary>
    private async Task ConvertScriptsAsync(IReadOnlyList<SavedScript> scripts, string folder)
    {
        _rerun = null;
        _readingFromText = false;
        var written = 0;
        var lastPath = string.Empty;

        for (var index = 0; index < scripts.Count; index++)
        {
            var script = scripts[index];
            string text;

            try
            {
                text = Scripts.Load(script.Title);
            }
            catch (IOException ex)
            {
                SetStatus($"Could not read {script.Title}: {ex.Message}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus($"{script.Title} has nothing in it, so no file was written.");
                continue;
            }

            SetStatus($"Writing {index + 1} of {scripts.Count}: {script.Title}");

            lastPath = FreeAudioPath(folder, script.Title);
            var outcome = await RunSynthesisAsync(() => new StringReader(text), lastPath, text.Length);

            if (outcome == RunOutcome.Stopped)
            {
                SetStatus($"Stopped. {written} of {scripts.Count} script(s) were written.");
                return;
            }

            if (outcome == RunOutcome.Finished) written++;
        }

        if (written == 0)
        {
            SetStatus("Nothing was written.");
            return;
        }

        SetStatusWithFileLink($"Wrote {written} of {scripts.Count} script(s) to ", lastPath);
    }

    /// <summary>A name nothing is using, so a batch never writes over a file already there.</summary>
    private string FreeAudioPath(string folder, string title)
    {
        var stem = AudioStem(title);
        var extension = _settings.OutputFormat == AudioOutputFormat.Mp3 ? ".mp3" : ".wav";

        var path = Path.Combine(folder, stem + extension);
        for (var next = 2; File.Exists(path); next++)
        {
            path = Path.Combine(folder, $"{stem} ({next}){extension}");
        }

        return path;
    }
}
