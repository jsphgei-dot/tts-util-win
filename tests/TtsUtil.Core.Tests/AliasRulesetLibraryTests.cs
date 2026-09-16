using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class AliasRulesetLibraryTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "TtsUtilWinRulesets", Guid.NewGuid().ToString("N"));

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

    /// <summary>A saved ruleset reads back under its name, with the list it came from forgotten.</summary>
    [Fact]
    public void ASavedRulesetReadsBackByName()
    {
        var library = new AliasRulesetLibrary(_root);

        library.Save("Work words", new[]
        {
            new AliasRule { Match = "SQL", SayAs = "sequel", Source = "chemistry" },
        });

        Assert.Equal(new[] { "Work words" }, library.Titles());

        var rule = Assert.Single(library.Load("Work words")!.Rules);
        Assert.Equal("sequel", rule.SayAs);
        Assert.Equal(string.Empty, rule.Source);

        Assert.True(library.Delete("Work words"));
        Assert.Empty(library.Titles());
    }

    [Fact]
    public void AnIdSaysWhichRulesetARuleCameFrom()
    {
        Assert.Equal("Work words", AliasRulesetLibrary.TitleOf(AliasRulesetLibrary.IdFor("Work words")));
        Assert.Null(AliasRulesetLibrary.TitleOf("chemistry"));
    }
}
