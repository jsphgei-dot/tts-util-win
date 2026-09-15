using Xunit;

namespace TtsUtil.Core.Tests;

public class AppVersionTests
{
    [Fact]
    public void TheVersionNameIsSemantic()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$", AppVersion.Name);
    }

    [Fact]
    public void TheVersionNameCarriesNoBuildMetadata()
    {
        Assert.DoesNotContain("+", AppVersion.Name);
    }

    [Fact]
    public void TheVersionCodeIsAPositiveInteger()
    {
        Assert.True(AppVersion.Code > 0, $"Version code was {AppVersion.Code}.");
    }

}
