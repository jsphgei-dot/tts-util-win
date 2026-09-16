/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Text.Json;

namespace TtsUtil.Core.Settings;

/// <summary>
/// The Text tab as the user last left it, every open document in its own file beside the settings
/// so that a whole chapter of pasted text does not have to live inside settings.json.
/// </summary>
public sealed class TextDraft
{
    public const string FileName = "drafts.json";

    private static readonly JsonSerializerOptions Format = new() { WriteIndented = true };

    private readonly string _path;

    public TextDraft(string path) => _path = path;

    /// <summary>The drafts that belong with a settings file.</summary>
    public static TextDraft Beside(string? settingsPath)
    {
        var folder = string.IsNullOrWhiteSpace(settingsPath)
            ? AppSettings.AppDirectory
            : Path.GetDirectoryName(settingsPath!);

        return new TextDraft(Path.Combine(folder ?? ".", FileName));
    }

    /// <summary>What was left behind, or nothing at all when there is no draft to read.</summary>
    public IReadOnlyList<DraftDocument> Read()
    {
        try
        {
            if (!File.Exists(_path)) return Array.Empty<DraftDocument>();

            return JsonSerializer.Deserialize<List<DraftDocument>>(File.ReadAllText(_path))
                ?? (IReadOnlyList<DraftDocument>)Array.Empty<DraftDocument>();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return Array.Empty<DraftDocument>();
        }
    }

    /// <summary>Keeps the documents for next time. Empty boxes leave no file behind.</summary>
    public void Write(IReadOnlyList<DraftDocument> documents)
    {
        try
        {
            var worth = documents.Where(document => !string.IsNullOrEmpty(document.Text)).ToList();
            if (worth.Count == 0)
            {
                if (File.Exists(_path)) File.Delete(_path);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(worth, Format));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A draft is a convenience, and a read only folder is not worth an error.
        }
    }
}

/// <summary>One text document as it was left: its tab name, the script it holds and the words.</summary>
public sealed class DraftDocument
{
    public string Title { get; set; } = string.Empty;

    public string? ScriptTitle { get; set; }

    public string Text { get; set; } = string.Empty;
}
