/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Settings;

/// <summary>
/// The Text tab as the user last left it, kept in its own file beside the settings so that a
/// whole chapter of pasted text does not have to live inside settings.json.
/// </summary>
public sealed class TextDraft
{
    public const string FileName = "draft.txt";

    private readonly string _path;

    public TextDraft(string path) => _path = path;

    /// <summary>The draft that belongs with a settings file.</summary>
    public static TextDraft Beside(string? settingsPath)
    {
        var folder = string.IsNullOrWhiteSpace(settingsPath)
            ? AppSettings.AppDirectory
            : Path.GetDirectoryName(settingsPath!);

        return new TextDraft(Path.Combine(folder ?? ".", FileName));
    }

    /// <summary>What was left behind, or nothing at all when there is no draft to read.</summary>
    public string Read()
    {
        try
        {
            return File.Exists(_path) ? File.ReadAllText(_path) : string.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    /// <summary>Keeps the text for next time. An empty box leaves no file behind.</summary>
    public void Write(string? text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
            {
                if (File.Exists(_path)) File.Delete(_path);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, text);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A draft is a convenience, and a read only folder is not worth an error.
        }
    }
}
