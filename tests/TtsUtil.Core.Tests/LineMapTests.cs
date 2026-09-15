using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class LineMapTests
{
    [Fact]
    public void MixedLineEndingsAllSplitOnceEach()
    {
        var map = LineMap.Build("one\r\ntwo\nthree\rfour");

        Assert.Equal(4, map.Count);
        Assert.Equal(new[] { "one", "two", "three", "four" }, map.Lines.Select(l => l.Text));
    }

    [Fact]
    public void OffsetsPointBackIntoTheOriginalText()
    {
        const string text = "one\r\ntwo\nthree";
        var map = LineMap.Build(text);

        foreach (var line in map.Lines)
        {
            Assert.Equal(line.Text, text.Substring(line.Start, line.Length));
        }
    }

    [Fact]
    public void BlankLinesAreKeptSoTheNumbersMatchTheEditor()
    {
        var map = LineMap.Build("one\n\nthree");

        Assert.Equal(3, map.Count);
        Assert.True(map.IsBlank(1));
        Assert.False(map.IsBlank(0));
    }

    [Fact]
    public void ATrailingBreakDoesNotAddAnEmptyLastLine()
    {
        Assert.Equal(2, LineMap.Build("one\ntwo\n").Count);
        Assert.Equal(2, LineMap.Build("one\r\ntwo\r\n").Count);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(5, 1)]
    [InlineData(7, 1)]
    [InlineData(9, 2)]
    [InlineData(999, 2)]
    public void AnOffsetResolvesToTheLineHoldingIt(long offset, int expectedLine)
    {
        // "one\r\ntwo\nthree": line 0 at 0, line 1 at 5, line 2 at 9.
        var map = LineMap.Build("one\r\ntwo\nthree");

        Assert.Equal(expectedLine, map.LineAt(offset));
    }

    [Fact]
    public void StartingAtALineGivesTheSubstringThatLineBegins()
    {
        const string text = "one\ntwo\nthree";
        var map = LineMap.Build(text);

        Assert.Equal("two\nthree", text[map.StartOf(1)..]);
    }
}
