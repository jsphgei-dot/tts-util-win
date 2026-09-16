using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class VoiceCatalogueLanguageTests
{
    [Fact]
    public void OnlyEnglishVoicesAreOfferedByDefault()
    {
        var recommended = DownloadableVoices.All.Where(voice => voice.IsRecommended);

        Assert.All(recommended, voice => Assert.StartsWith("English", voice.Language, StringComparison.Ordinal));
    }

    [Fact]
    public void TheScriptsTheStrictSettingWouldSilenceAreFlagged()
    {
        var cyrillicOrBeyond = new[] { "ru_RU", "uk_UA", "el_GR", "ar_JO", "hi_IN", "zh" };

        foreach (var voice in DownloadableVoices.All)
        {
            var expected = cyrillicOrBeyond.Any(marker => voice.Id.Contains(marker, StringComparison.Ordinal));

            Assert.Equal(expected, voice.NonLatinScript);
        }
    }
}
