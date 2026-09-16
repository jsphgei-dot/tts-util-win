/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using TtsUtil.Core.Settings;

namespace TtsUtil.Core.Text;

/// <summary>Rulesets of your own, saved by name and ticked like the lists that ship.</summary>
public sealed class AliasRulesetLibrary
{
    public const string FolderName = "rulesets";

    public const string Extension = ".json";

    /// <summary>What marks a rule as one a saved ruleset put in the grid.</summary>
    public const string IdPrefix = "saved:";

    public AliasRulesetLibrary(string directory) => Directory = directory;

    public string Directory { get; }

    /// <summary>The rulesets beside a settings file at this path.</summary>
    public static AliasRulesetLibrary Beside(string settingsPath) =>
        new(Path.Combine(Path.GetDirectoryName(settingsPath) ?? ".", FolderName));

    public static string IdFor(string title) => IdPrefix + title;

    /// <summary>The title an id names, or null when the id is not a saved ruleset.</summary>
    public static string? TitleOf(string id) =>
        id.StartsWith(IdPrefix, StringComparison.OrdinalIgnoreCase) ? id[IdPrefix.Length..] : null;

    /// <summary>Every saved ruleset, by name, in the order they read.</summary>
    public IReadOnlyList<string> Titles()
    {
        if (!System.IO.Directory.Exists(Directory)) return Array.Empty<string>();

        var titles = System.IO.Directory.EnumerateFiles(Directory, "*" + Extension)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(title => !string.IsNullOrEmpty(title))
            .Select(title => title!)
            .ToList();

        titles.Sort(StringComparer.CurrentCultureIgnoreCase);
        return titles;
    }

    public bool Exists(string title) => PathFor(title) is string path && File.Exists(path);

    /// <summary>Writes the rules under this name, replacing a ruleset already called that.</summary>
    public bool Save(string title, IEnumerable<AliasRule> rules)
    {
        if (PathFor(title) is not string path) return false;

        var kept = rules.Select(rule =>
        {
            var copy = rule.Copy();
            copy.Source = string.Empty;
            return copy;
        }).ToList();

        System.IO.Directory.CreateDirectory(Directory);
        File.WriteAllText(path, new AliasDictionary { Name = title, Rules = kept }.ToJson());
        return true;
    }

    /// <summary>The ruleset of this name, or null when there is none or it will not read.</summary>
    public AliasDictionary? Load(string title)
    {
        try
        {
            if (PathFor(title) is not string path || !File.Exists(path)) return null;

            return AliasDictionary.FromJson(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public bool Delete(string title)
    {
        if (PathFor(title) is not string path || !File.Exists(path)) return false;

        File.Delete(path);
        return true;
    }

    /// <summary>The file a name maps to, or null when nothing usable is left of the name.</summary>
    public string? PathFor(string title)
    {
        var name = ScriptLibrary.ToFileName(title);
        return name is null ? null : Path.Combine(Directory, name + Extension);
    }
}
