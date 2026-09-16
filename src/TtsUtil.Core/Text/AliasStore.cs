/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Text;

/// <summary>Reads and writes the alias list, kept in its own file beside the settings.</summary>
public sealed class AliasStore
{
    public const string FileName = "aliases.json";

    private readonly string _path;

    public AliasStore(string path) => _path = path;

    /// <summary>The list beside a settings file at this path.</summary>
    public static AliasStore Beside(string settingsPath) =>
        new(Path.Combine(Path.GetDirectoryName(settingsPath) ?? ".", FileName));

    public string FilePath => _path;

    /// <summary>Reads the list, or an empty one when there is no file or it will not parse.</summary>
    public AliasDictionary Load()
    {
        try
        {
            return File.Exists(_path)
                ? AliasDictionary.FromJson(File.ReadAllText(_path)) ?? new AliasDictionary()
                : new AliasDictionary();
        }
        catch (IOException)
        {
            return new AliasDictionary();
        }
        catch (UnauthorizedAccessException)
        {
            return new AliasDictionary();
        }
    }

    public void Save(AliasDictionary aliases)
    {
        var folder = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        File.WriteAllText(_path, aliases.ToJson());
    }
}
