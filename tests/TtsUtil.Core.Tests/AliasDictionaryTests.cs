using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class AliasDictionaryTests
{
    [Fact]
    public void AnAliasIsSpokenInPlaceOfWhatWasTyped()
    {
        var aliases = Dictionary(Rule("SQL", "sequel"));

        Assert.Equal("the sequel server", aliases.Apply("the SQL server"));
    }

    [Fact]
    public void AWholeWordRuleDoesNotFireInsideALongerWord()
    {
        var aliases = Dictionary(Rule("dr", "doctor"));

        Assert.Equal("doctor sees the dry run", aliases.Apply("dr sees the dry run"));
    }

    [Fact]
    public void TurningOffWholeWordReachesInsideWords()
    {
        var rule = Rule("cat", "dog");
        rule.WholeWord = false;

        Assert.Equal("dogalogue", Dictionary(rule).Apply("catalogue"));
    }

    [Fact]
    public void ASymbolMatchesWithNoWordBoundaryAroundIt()
    {
        var aliases = Dictionary(Rule("&", " and "));

        Assert.Equal("R and D", aliases.Apply("R&D"));
    }

    [Fact]
    public void MatchCaseSeparatesTwoSpellingsOfTheSameWord()
    {
        var upper = Rule("IT", "eye tee");
        upper.MatchCase = true;

        Assert.Equal("eye tee says it works", Dictionary(upper).Apply("IT says it works"));
    }

    [Fact]
    public void ALaterRuleDoesNotChewThroughAnEarlierReplacement()
    {
        // The second rule would fire on the word the first one just wrote.
        var aliases = Dictionary(Rule("Dr", "doctor"), Rule("doctor", "physician"));

        Assert.Equal("doctor Who", aliases.Apply("Dr Who"));
    }

    [Fact]
    public void RulesRunInTheOrderTheyAreListed()
    {
        var aliases = Dictionary(Rule("a", "b"), Rule("b", "c"));

        Assert.Equal("b c", aliases.Apply("a b"));
    }

    [Fact]
    public void ADisabledRuleIsSkipped()
    {
        var rule = Rule("SQL", "sequel");
        rule.Enabled = false;

        Assert.Equal("SQL", Dictionary(rule).Apply("SQL"));
    }

    [Fact]
    public void AListSurvivesBeingWrittenAndReadBack()
    {
        var aliases = Dictionary(Rule("SQL", "sequel"));
        aliases.Name = "Work words";

        var read = AliasDictionary.FromJson(aliases.ToJson());

        Assert.NotNull(read);
        Assert.Equal("Work words", read!.Name);
        Assert.Equal("sequel", Assert.Single(read.Rules).SayAs);
    }

    [Fact]
    public void RubbishIsRejectedRatherThanThrown()
    {
        Assert.Null(AliasDictionary.FromJson("not json at all"));
    }

    [Fact]
    public void AFileFromALaterVersionIsRefused()
    {
        Assert.Null(AliasDictionary.FromJson("{\"version\": 99, \"rules\": []}"));
    }

    [Fact]
    public void MergingAddsWhatIsNewAndLeavesWhatIsAlreadyThere()
    {
        var mine = Dictionary(Rule("SQL", "sequel"));
        var theirs = Dictionary(Rule("sql", "ess queue ell"), Rule("GIF", "jif"));

        var added = mine.Merge(theirs);

        Assert.Equal(1, added);
        Assert.Equal(new[] { "sequel", "jif" }, mine.Rules.Select(rule => rule.SayAs));
    }

    private static AliasDictionary Dictionary(params AliasRule[] rules) => new() { Rules = rules.ToList() };

    private static AliasRule Rule(string match, string sayAs) => new() { Match = match, SayAs = sayAs };
}
