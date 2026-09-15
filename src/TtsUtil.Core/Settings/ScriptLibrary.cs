/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Text;

namespace TtsUtil.Core.Settings;

/// <summary>One saved script, which is a plain text file the user can also edit elsewhere.</summary>
public sealed class SavedScript
{
    public string Title { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public DateTime Modified { get; init; }

    public long Characters { get; init; }

    public override string ToString() => $"{Title} ({Characters:N0} characters, {Modified:yyyy-MM-dd HH:mm})";
}

/// <summary>Saved scripts, stored as text files so nothing is trapped in an app format.</summary>
public sealed class ScriptLibrary
{
    public const string FolderName = "scripts";

    public const string Extension = ".txt";

    private static readonly char[] Invalid =
        System.IO.Path.GetInvalidFileNameChars().Concat(new[] { '.' }).Distinct().ToArray();

    public ScriptLibrary(string directory) => Directory = directory;

    public string Directory { get; }

    /// <summary>Beside the executable when portable, otherwise under the roaming profile.</summary>
    public static string ResolveDirectory(string appDirectory, string roamingAppData, Func<string, bool> fileExists)
    {
        if (InstallPaths.IsPortable(appDirectory, fileExists))
        {
            return System.IO.Path.Combine(appDirectory, FolderName);
        }

        return System.IO.Path.Combine(roamingAppData, InstallPaths.ApplicationFolderName, FolderName);
    }

    /// <summary>Turns a title into a file name, or null when nothing usable is left.</summary>
    public static string? ToFileName(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var builder = new StringBuilder(title!.Length);
        foreach (var c in title.Trim())
        {
            builder.Append(Invalid.Contains(c) ? ' ' : c);
        }

        // Collapse the gaps the stripping leaves, then trim what Windows will not keep.
        var cleaned = string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        cleaned = cleaned.Trim().TrimEnd('.');

        if (cleaned.Length == 0) return null;
        return cleaned.Length > 100 ? cleaned[..100].TrimEnd() : cleaned;
    }

    /// <summary>Every saved script, most recently changed first.</summary>
    public IReadOnlyList<SavedScript> List()
    {
        if (!System.IO.Directory.Exists(Directory)) return Array.Empty<SavedScript>();

        var scripts = new List<SavedScript>();

        foreach (var path in System.IO.Directory.EnumerateFiles(Directory, "*" + Extension))
        {
            var info = new FileInfo(path);
            scripts.Add(new SavedScript
            {
                Title = System.IO.Path.GetFileNameWithoutExtension(path),
                Path = path,
                Modified = info.LastWriteTime,
                Characters = info.Length,
            });
        }

        scripts.Sort((a, b) => b.Modified.CompareTo(a.Modified));
        return scripts;
    }

    public bool Exists(string title) => File.Exists(PathFor(title));

    /// <summary>Writes the script, replacing any script of the same title.</summary>
    public SavedScript Save(string title, string text)
    {
        var path = PathFor(title);
        System.IO.Directory.CreateDirectory(Directory);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var info = new FileInfo(path);
        return new SavedScript
        {
            Title = System.IO.Path.GetFileNameWithoutExtension(path),
            Path = path,
            Modified = info.LastWriteTime,
            Characters = info.Length,
        };
    }

    public string Load(string title) => File.ReadAllText(PathFor(title));

    public void Delete(string title)
    {
        var path = PathFor(title);
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>Renames a script, refusing to write over one that already exists.</summary>
    public bool Rename(string oldTitle, string newTitle)
    {
        var from = PathFor(oldTitle);
        var to = PathFor(newTitle);

        if (!File.Exists(from)) return false;
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase)) return true;
        if (File.Exists(to)) return false;

        File.Move(from, to);
        return true;
    }

    /// <summary>The file a title maps to, which is always inside the library directory.</summary>
    public string PathFor(string title)
    {
        var name = ToFileName(title) ?? throw new ArgumentException("A script needs a title.", nameof(title));
        return System.IO.Path.Combine(Directory, name + Extension);
    }
}
