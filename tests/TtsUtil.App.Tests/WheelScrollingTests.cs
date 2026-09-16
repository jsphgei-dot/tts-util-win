using Xunit;

namespace TtsUtil.App.Tests;

public sealed class WheelScrollingTests
{
    /// <summary>The page takes the wheel at either end of a scroller, and whenever that scroller
    /// has nothing to scroll at all.</summary>
    [Fact]
    public void TheWheelIsHandedOnOnlyWhenThereIsNoRoomLeft()
    {
        Assert.True(WheelScrolling.OutOfRoom(offset: 0, scrollable: 400, delta: 120));
        Assert.True(WheelScrolling.OutOfRoom(offset: 400, scrollable: 400, delta: -120));
        Assert.True(WheelScrolling.OutOfRoom(offset: 0, scrollable: 0, delta: -120));

        Assert.False(WheelScrolling.OutOfRoom(offset: 0, scrollable: 400, delta: -120));
        Assert.False(WheelScrolling.OutOfRoom(offset: 200, scrollable: 400, delta: 120));
    }
}
