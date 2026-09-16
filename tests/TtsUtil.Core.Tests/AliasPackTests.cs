using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class AliasPackTests
{
    [Fact]
    public void ChemistryReadsElementSymbolsByName()
    {
        var chemistry = AliasPacks.Find("chemistry")!.ToDictionary();

        Assert.Equal("potassium and calcium", chemistry.Apply("K and Ca"));
        Assert.Equal("water and carbon dioxide", chemistry.Apply("H2O and CO2"));
    }

    /// <summary>Case matters, and the symbols that are English words arrive turned off.</summary>
    [Fact]
    public void ChemistryLeavesOrdinaryWordsAlone()
    {
        var chemistry = AliasPacks.Find("chemistry")!.ToDictionary();

        Assert.Equal("In no case was he calcium", chemistry.Apply("In no case was he Ca"));
        Assert.Equal("a can of ca", chemistry.Apply("a can of ca"));
    }

    [Fact]
    public void OnlyTheListsThatAreAskedForContributeRules()
    {
        Assert.Empty(AliasPacks.RulesFor(Array.Empty<string>()));
        Assert.NotEmpty(AliasPacks.RulesFor(new[] { "math" }));
        Assert.Empty(AliasPacks.RulesFor(new[] { "no-such-list" }));
    }
}
