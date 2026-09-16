using TtsUtil.Core.Settings;
using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class SettingsHelpTests
{
    [Fact]
    public void TheExampleInTheExtraCharactersHelpBehavesAsItSays()
    {
        const string example = "%$&+";
        Assert.Contains(example, SettingsHelp.Find(SettingsHelp.AllowedExtra)!.Body);

        var options = new SpokenCharacterOptions { AllowedExtra = example };

        Assert.Equal("50% off", SpokenCharacters.Clean("50% off", options));
        Assert.Equal("$20", SpokenCharacters.Clean("$20", options));
        Assert.Equal("Smith & Co", SpokenCharacters.Clean("Smith & Co", options));
        Assert.Equal("2+2", SpokenCharacters.Clean("2+2", options));
    }

    [Fact]
    public void ACommaTypedInExtraCharactersIsACharacterRatherThanASeparator()
    {
        var separated = new SpokenCharacterOptions { AllowedExtra = "%, $" };

        // The help says spaces and commas do nothing, so a hash stays out and a percent gets through.
        Assert.Equal("100% #1", SpokenCharacters.Clean("100% #1", new SpokenCharacterOptions { AllowedExtra = "%#" }));
        Assert.Equal("100% 1", SpokenCharacters.Clean("100% #1", separated));
    }
}
