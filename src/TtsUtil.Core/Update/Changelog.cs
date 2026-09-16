/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Text;

namespace TtsUtil.Core.Update;

/// <summary>One release and what changed in it.</summary>
public sealed record ChangelogRelease(string Title, IReadOnlyList<string> Notes);

/// <summary>Reads the shipped changelog into releases the window can list.</summary>
public static class Changelog
{
    /// <summary>The releases in the order they are written, which is newest first.</summary>
    public static IReadOnlyList<ChangelogRelease> Parse(string markdown)
    {
        var releases = new List<ChangelogRelease>();
        var notes = new List<string>();
        var note = new StringBuilder();
        var title = string.Empty;

        void CloseNote()
        {
            if (note.Length == 0) return;

            notes.Add(note.ToString());
            note.Clear();
        }

        void CloseRelease()
        {
            CloseNote();
            if (title.Length > 0) releases.Add(new ChangelogRelease(title, notes.ToList()));

            notes.Clear();
        }

        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim().Replace("**", string.Empty);

            if (raw.StartsWith("## ", StringComparison.Ordinal))
            {
                CloseRelease();
                title = line[3..];
                continue;
            }

            if (line.StartsWith("* ", StringComparison.Ordinal) || line.StartsWith("- ", StringComparison.Ordinal))
            {
                CloseNote();
                note.Append(line[2..]);
                continue;
            }

            // A wrapped bullet carries on underneath itself, indented and with nothing between.
            if (line.Length == 0) CloseNote();
            else if (note.Length > 0) note.Append(' ').Append(line);
        }

        CloseRelease();

        return releases;
    }
}
