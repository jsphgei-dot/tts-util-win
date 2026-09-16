using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class ScriptLibraryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ttsutil-scripts-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptLibrary _library;

    public ScriptLibraryTests()
    {
        Directory.CreateDirectory(_root);
        _library = new ScriptLibrary(_root);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Theory]
    [InlineData("Chapter 1: the beginning", "Chapter 1 the beginning")]
    [InlineData("a/b\\c", "a b c")]
    [InlineData("  padded  ", "padded")]
    [InlineData("trailing dots...", "trailing dots")]
    [InlineData("///", null)]
    [InlineData("   ", null)]
    public void TitlesBecomeFileNamesWindowsWillAccept(string title, string? expected)
    {
        Assert.Equal(expected, ScriptLibrary.ToFileName(title));
    }

    [Fact]
    public void ATitleCannotEscapeTheLibraryDirectory()
    {
        var path = _library.PathFor("../../escape");

        Assert.Equal(_root, Path.GetDirectoryName(path));
    }

    [Fact]
    public void ScriptsRoundTripThroughPlainTextFiles()
    {
        _library.Save("Notes", "Line one.\nLine two, with café and 你好.");

        Assert.Equal("Line one.\nLine two, with café and 你好.", _library.Load("Notes"));
        Assert.True(_library.Exists("Notes"));
    }

    [Fact]
    public void SavingTheSameTitleReplacesRatherThanDuplicates()
    {
        _library.Save("Notes", "first");
        _library.Save("Notes", "second");

        Assert.Single(_library.List());
        Assert.Equal("second", _library.Load("Notes"));
    }

    [Fact]
    public void TheListPutsTheMostRecentlyChangedFirst()
    {
        _library.Save("Older", "a");
        File.SetLastWriteTime(_library.PathFor("Older"), DateTime.Now.AddHours(-2));
        _library.Save("Newer", "b");

        Assert.Equal(new[] { "Newer", "Older" }, _library.List().Select(s => s.Title));
    }

    [Fact]
    public void ATextFileDroppedIntoTheFolderIsPickedUp()
    {
        File.WriteAllText(Path.Combine(_root, "Written by hand.txt"), "outside the program");

        var found = _library.List().Single();

        Assert.Equal("Written by hand", found.Title);
        Assert.Equal("outside the program", _library.Load("Written by hand"));
    }

    [Fact]
    public void RenamingRefusesToOverwriteAnExistingScript()
    {
        _library.Save("Keep", "keep me");
        _library.Save("Other", "other");

        Assert.False(_library.Rename("Other", "Keep"));
        Assert.Equal("keep me", _library.Load("Keep"));
        Assert.True(_library.Exists("Other"));
    }

    [Fact]
    public void RenamingMovesTheFileAndLeavesNothingBehind()
    {
        _library.Save("Before", "text");

        Assert.True(_library.Rename("Before", "After"));
        Assert.False(_library.Exists("Before"));
        Assert.Equal("text", _library.Load("After"));
    }

    [Fact]
    public void TheVoiceAndSpeedFollowAScriptThroughRenamingAndDeleting()
    {
        _library.Save("Act One", "words");
        _library.SaveProperties("Act One", new ScriptProperties { VoiceName = "David", SpeakerId = 3, Speed = 1.25f });

        Assert.True(_library.Rename("Act One", "Act Two"));

        var kept = _library.LoadProperties("Act Two");
        Assert.NotNull(kept);
        Assert.Equal("David", kept!.VoiceName);
        Assert.Equal(3, kept.SpeakerId);
        Assert.Equal(1.25f, kept.Speed);

        _library.Delete("Act Two");
        Assert.Null(_library.LoadProperties("Act Two"));
        Assert.Empty(_library.List());
    }

    [Fact]
    public void APortableInstallKeepsItsScriptsBesideTheExecutable()
    {
        var portable = ScriptLibrary.ResolveDirectory(@"C:\portable", @"C:\roaming", _ => true);
        var installed = ScriptLibrary.ResolveDirectory(@"C:\program", @"C:\roaming", _ => false);

        Assert.Equal(Path.Combine(@"C:\portable", "scripts"), portable);
        Assert.Equal(Path.Combine(@"C:\roaming", "TtsUtilWin", "scripts"), installed);
    }
}
