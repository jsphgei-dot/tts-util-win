using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class HistoryPagingTests
{
    [Fact]
    public void PageOneHoldsTheNewestEntries()
    {
        Assert.Equal(new[] { "e", "d" }, HistoryPaging.Page(NewestFirst(), 1, 2));
    }

    [Fact]
    public void LaterPagesGoFurtherBackInTime()
    {
        Assert.Equal(new[] { "c", "b" }, HistoryPaging.Page(NewestFirst(), 2, 2));
    }

    [Fact]
    public void TheLastPageHoldsWhatIsLeftOver()
    {
        Assert.Equal(new[] { "a" }, HistoryPaging.Page(NewestFirst(), 3, 2));
    }

    [Theory]
    [InlineData(0, 10, 1)]
    [InlineData(5, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    public void TheCountIsNeverBelowOnePage(int items, int pageSize, int expected)
    {
        Assert.Equal(expected, HistoryPaging.PageCount(items, pageSize));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-4, 1)]
    [InlineData(99, 3)]
    public void APageOutsideTheRangeClamps(int asked, int expected)
    {
        Assert.Equal(expected, HistoryPaging.ClampPage(asked, 3));
    }

    private static string[] NewestFirst() => new[] { "e", "d", "c", "b", "a" };
}
