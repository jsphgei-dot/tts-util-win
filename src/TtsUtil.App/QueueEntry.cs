/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.App;

/// <summary>How a reading ended, which is what decides whether the queue carries on.</summary>
public enum RunOutcome
{
    Finished,
    Stopped,
    Failed,
}

/// <summary>One item waiting to be read, either a saved script or the Text tab itself.</summary>
public sealed class QueueEntry
{
    public QueueEntry(string title, string? scriptTitle)
    {
        Title = title;
        ScriptTitle = scriptTitle;
    }

    public string Title { get; }

    /// <summary>The saved script to read, or null for whatever is in the Text tab.</summary>
    public string? ScriptTitle { get; }

    public static QueueEntry ForTextTab() => new("Text tab", null);

    public static QueueEntry ForScript(string title) => new(title, title);

    public override string ToString() => Title;
}
