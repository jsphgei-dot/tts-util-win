using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public class TypingReaderTests
{
    [Fact]
    public void SingleOrdinaryCharacterIsReadOnItsOwn()
    {
        Assert.Equal("a", TypingReader.TextToRead("a", 0, 1, 0));
    }

    [Fact]
    public void DelimiterReadsBackTheWordJustTyped()
    {
        Assert.Equal("cat ", TypingReader.TextToRead("cat ", 3, 1, 0));
    }

    [Fact]
    public void SecondDelimiterInARowReadsNothing()
    {
        Assert.Null(TypingReader.TextToRead("cat  ", 4, 1, 0));
    }

    [Fact]
    public void DeletionsAreIgnored()
    {
        Assert.Null(TypingReader.TextToRead("ca", 2, 0, 1));
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
