using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public class TypingReaderTests
{
    [Fact]
    public void AnOrdinaryCharacterSaysNothingOnItsOwn()
    {
        Assert.Null(TypingReader.TextToRead("a", 0, 1, 0));
        Assert.Null(TypingReader.TextToRead("ca", 1, 1, 0));
    }

    [Fact]
    public void ASpaceReadsBackTheWordJustTyped()
    {
        Assert.Equal("cat ", TypingReader.TextToRead("cat ", 3, 1, 0));
    }

    [Theory]
    [InlineData(".")]
    [InlineData(",")]
    [InlineData("?")]
    [InlineData("!")]
    [InlineData(";")]
    [InlineData(":")]
    [InlineData("\n")]
    public void AnyDelimiterCompletesTheWord(string delimiter)
    {
        var text = "cat" + delimiter;

        Assert.Equal(text, TypingReader.TextToRead(text, 3, 1, 0));
    }

    [Fact]
    public void OnlyTheLastWordIsRead()
    {
        Assert.Equal("dog ", TypingReader.TextToRead("the cat and a dog ", 17, 1, 0));
    }

    [Fact]
    public void SecondDelimiterInARowReadsNothing()
    {
        Assert.Null(TypingReader.TextToRead("cat  ", 4, 1, 0));
        Assert.Null(TypingReader.TextToRead("cat. ", 4, 1, 0));
    }

    [Fact]
    public void ADelimiterAtTheStartOfTheInputReadsNothing()
    {
        Assert.Null(TypingReader.TextToRead(" ", 0, 1, 0));
    }

    [Fact]
    public void TypingAWordSpeaksOnceAtTheEnd()
    {
        var spoken = new List<string>();
        var text = string.Empty;

        // Simulate one keystroke at a time, exactly as the text box reports them.
        foreach (var c in "cat dog ")
        {
            var offset = text.Length;
            text += c;
            var snippet = TypingReader.TextToRead(text, offset, 1, 0);
            if (snippet is not null) spoken.Add(snippet);
        }

        Assert.Equal(new[] { "cat ", "dog " }, spoken);
    }

    [Fact]
    public void DeletionsAreIgnored()
    {
        Assert.Null(TypingReader.TextToRead("ca", 2, 0, 1));
        Assert.Null(TypingReader.TextToRead("ca", 0, 2, 3));
    }

    [Fact]
    public void WholesaleReplacementIsReadInFull()
    {
        Assert.Equal("pasted text", TypingReader.TextToRead("pasted text", 0, 11, 3));
    }

    [Fact]
    public void InsertionInTheMiddleIsIgnored()
    {
        Assert.Null(TypingReader.TextToRead("abcdef", 2, 3, 0));
    }

    [Fact]
    public void OutOfRangeChangesAreIgnored()
    {
        Assert.Null(TypingReader.TextToRead("abc", 2, 9, 0));
    }
}
