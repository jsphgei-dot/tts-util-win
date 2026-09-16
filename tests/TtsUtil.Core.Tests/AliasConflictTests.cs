using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class AliasConflictTests
{
    [Fact]
    public void TheSameWordListedTwiceIsReported()
    {
        var clashes = AliasConflicts.Find(new[]
        {
            new AliasRule { Match = "GNU", SayAs = "gnew" },
            new AliasRule { Match = "gnu", SayAs = "guh noo" },
        });

        Assert.Equal(AliasClash.SameWord, Assert.Single(clashes).Clash);
    }

    /// <summary>A shorter rule above a longer one takes the text first, so the longer one is dead.</summary>
    [Fact]
    public void AnEarlierRuleThatEatsALaterOneIsReported()
    {
        var clashes = AliasConflicts.Find(new[]
        {
            new AliasRule { Match = "cat", SayAs = "feline", WholeWord = false },
            new AliasRule { Match = "catalog", SayAs = "list", WholeWord = false },
        });

        Assert.Equal(AliasClash.EatenByEarlier, Assert.Single(clashes).Clash);
    }

    /// <summary>Whole word keeps a short rule out of the middle of a longer one.</summary>
    [Fact]
    public void AWholeWordRuleLeavesALongerRuleAlone()
    {
        Assert.Null(AliasConflicts.Summarize(new[]
        {
            new AliasRule { Match = "cat", SayAs = "feline", WholeWord = true },
            new AliasRule { Match = "catalog", SayAs = "list", WholeWord = true },
        }));
    }

    [Fact]
    public void RulesThatAreOffAreNotCounted()
    {
        Assert.Null(AliasConflicts.Summarize(new[]
        {
            new AliasRule { Match = "GNU", SayAs = "gnew" },
            new AliasRule { Match = "GNU", SayAs = "guh noo", Enabled = false },
        }));
    }
}
