/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Text;

/// <summary>Pages a newest first list. Page 1 is the newest, so the numbers count backwards
/// in time and page 1 is always the way back to now.</summary>
public static class HistoryPaging
{
    /// <summary>At least one, so an empty list still reads as page 1 of 1.</summary>
    public static int PageCount(int itemCount, int pageSize)
    {
        if (pageSize < 1) return 1;

        return Math.Max(1, (itemCount + pageSize - 1) / pageSize);
    }

    public static int ClampPage(int page, int pageCount) => Math.Clamp(page, 1, Math.Max(1, pageCount));

    /// <summary>The slice on a page, counting from the newest end.</summary>
    public static IReadOnlyList<T> Page<T>(IReadOnlyList<T> newestFirst, int page, int pageSize)
    {
        if (pageSize < 1) return newestFirst;

        var wanted = ClampPage(page, PageCount(newestFirst.Count, pageSize));
        var from = (wanted - 1) * pageSize;

        if (from >= newestFirst.Count) return Array.Empty<T>();

        var take = Math.Min(pageSize, newestFirst.Count - from);
        var slice = new T[take];

        for (var at = 0; at < take; at++) slice[at] = newestFirst[from + at];

        return slice;
    }
}
