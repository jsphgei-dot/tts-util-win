using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public class TextToolsTests
{
    [Fact]
    public void BulletsGoOnEveryLineAndComeOffAgain()
    {
        var block = "One\nTwo";

        var bulleted = TextTools.ToggleBullets(block);

        Assert.Equal("• One\n• Two", bulleted);
        Assert.Equal(block, TextTools.ToggleBullets(bulleted));
    }

    [Fact]
    public void NumberingRenumbersRatherThanStackingPrefixes()
    {
        var numbered = TextTools.ToggleNumbers("3. Third\nSeventh");

        Assert.Equal("1. Third\n2. Seventh", numbered);
        Assert.Equal("Third\nSeventh", TextTools.ToggleNumbers(numbered));
    }

    [Fact]
    public void ABlankLineKeepsItsPlaceInAList()
    {
        Assert.Equal("1. One\n\n2. Two", TextTools.ToggleNumbers("One\n\nTwo"));
    }

    [Fact]
    public void OutdentTakesBackWhatIndentGave()
    {
        var indented = TextTools.Indent("One\nTwo");

        Assert.Equal("    One\n    Two", indented);
        Assert.Equal("One\nTwo", TextTools.Outdent(indented));
    }

    [Fact]
    public void OutdentAcceptsATabOrAPartialStep()
    {
        Assert.Equal("One\nTwo", TextTools.Outdent("\tOne\n  Two"));
    }

    [Theory]
    [InlineData(LetterCase.Upper, "THE BBC SAID SO. TWICE.")]
    [InlineData(LetterCase.Lower, "the bbc said so. twice.")]
    [InlineData(LetterCase.Sentence, "The BBC said so. Twice.")]
    [InlineData(LetterCase.Title, "The BBC Said So. Twice.")]
    public void CaseChangesLeaveAcronymsAlone(LetterCase letterCase, string expected)
    {
        Assert.Equal(expected, TextTools.ChangeCase("the BBC said so. twice.", letterCase));
    }

    [Fact]
    public void RunsOfBlankLinesBecomeOne()
    {
        Assert.Equal("One\n\nTwo", TextTools.CollapseBlankLines("One\n\n\n\nTwo"));
    }

    [Fact]
    public void WrappedLinesJoinButParagraphsAndListsDoNot()
    {
        var block = "A sentence that ran\nover two lines.\n\n• A bullet\n• Another";

        Assert.Equal("A sentence that ran over two lines.\n\n• A bullet\n• Another",
            TextTools.JoinWrappedLines(block));
    }

    [Fact]
    public void TidyingSpacingSquashesRunsAndTrailingBlanks()
    {
        Assert.Equal("One two\nthree", TextTools.TidySpacing("One    two   \nthree\t\t"));
    }

    [Fact]
    public void ReplaceAllCountsWhatItChangedAndCanIgnoreCase()
    {
        var (text, count) = TextTools.ReplaceAll("Cat cat CAT", "cat", "dog", caseSensitive: false);

        Assert.Equal("dog dog dog", text);
        Assert.Equal(3, count);
    }

    [Fact]
    public void ReplaceAllLeavesTextAloneWhenNothingMatches()
    {
        var (text, count) = TextTools.ReplaceAll("Cat", "dog", "cat", caseSensitive: true);

        Assert.Equal("Cat", text);
        Assert.Equal(0, count);
    }

    [Fact]
    public void WindowsLineEndingsSurviveEveryTool()
    {
        Assert.Equal("• One\r\n• Two", TextTools.ToggleBullets("One\r\nTwo"));
    }
}
