using Xunit;

namespace TtsUtil.App.Tests;

public class CommandLineTests
{
    [Fact]
    public void NoArgumentsMeansNoOverride()
    {
        Assert.Null(App.ReadVoicesArgument(Array.Empty<string>()));
        Assert.Null(App.ReadVoicesArgument(new[] { "--something", "else" }));
    }

    [Fact]
    public void TheSeparateFormIsRead()
    {
        Assert.Equal(@"D:\voices", App.ReadVoicesArgument(new[] { "--voices", @"D:\voices" }));
    }

    [Fact]
    public void TheEqualsFormIsRead()
    {
        Assert.Equal(@"D:\voices", App.ReadVoicesArgument(new[] { @"--voices=D:\voices" }));
    }

    [Fact]
    public void QuotesAndSpacesAreTrimmed()
    {
        Assert.Equal(@"D:\my voices", App.ReadVoicesArgument(new[] { "--voices", @"  ""D:\my voices""  " }));
    }

    [Fact]
    public void TheSwitchIsCaseInsensitive()
    {
        Assert.Equal(@"D:\voices", App.ReadVoicesArgument(new[] { "--VOICES", @"D:\voices" }));
    }

    [Fact]
    public void ADanglingSwitchIsIgnored()
    {
        Assert.Null(App.ReadVoicesArgument(new[] { "--voices" }));
        Assert.Null(App.ReadVoicesArgument(new[] { "--voices=" }));
    }
}
