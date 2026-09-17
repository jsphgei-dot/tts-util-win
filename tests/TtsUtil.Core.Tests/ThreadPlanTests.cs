using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class ThreadPlanTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(8, 4)]
    [InlineData(16, 4)]
    public void WithNoSettingItTakesHalfTheProcessorsUpToFour(int processors, int expected) =>
        Assert.Equal(expected, ThreadPlan.Resolve(null, processors));

    [Theory]
    [InlineData(32, 8)]
    [InlineData(64, 8)]
    public void AWideMachineIsAllowedTwiceTheAutomaticCeiling(int processors, int expected) =>
        Assert.Equal(expected, ThreadPlan.Resolve(null, processors));

    [Theory]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    [InlineData(8, 8)]
    [InlineData(16, 8)]
    [InlineData(64, 16)]
    public void The32BitBuildDoublesBothEndsOfTheAutomaticRange(int processors, int expected) =>
        Assert.Equal(expected, ThreadPlan.Resolve(null, processors, x86: true));

    [Fact]
    public void AChosenCountIsUsedWhateverTheMachineHas() =>
        Assert.Equal(12, ThreadPlan.Resolve(12, 4));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(99, 16)]
    public void AChosenCountIsHeldInsideTheAllowedRange(int chosen, int expected) =>
        Assert.Equal(expected, ThreadPlan.Resolve(chosen, 8));
}
