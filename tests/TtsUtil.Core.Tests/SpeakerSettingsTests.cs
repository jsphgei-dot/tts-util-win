using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class SpeakerSettingsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ttsutil-spkset-" + Guid.NewGuid().ToString("N"));

    public SpeakerSettingsTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void AnUnknownVoiceFallsBackToTheSharedSpeakerId()
    {
        var settings = new AppSettings { SpeakerId = 7 };

        Assert.Equal(7, settings.GetSpeakerId("never-seen"));
    }

    [Fact]
    public void EachVoiceRemembersItsOwnSpeaker()
    {
        var settings = new AppSettings();

        settings.SetSpeakerId("libritts", 512);
        settings.SetSpeakerId("vctk", 3);

        Assert.Equal(512, settings.GetSpeakerId("libritts"));
        Assert.Equal(3, settings.GetSpeakerId("vctk"));
    }

    [Fact]
    public void VoiceNamesAreMatchedWithoutRegardToCase()
    {
        var settings = new AppSettings();

        settings.SetSpeakerId("LibriTTS", 12);

        Assert.Equal(12, settings.GetSpeakerId("libritts"));
    }

    [Fact]
    public void TheSharedSpeakerIdTracksTheLastChoiceForANewVoice()
    {
        var settings = new AppSettings();

        settings.SetSpeakerId("libritts", 44);

        Assert.Equal(44, settings.GetSpeakerId("a-voice-never-used-before"));
    }

    [Fact]
    public void StarringIsPerVoiceAndReversible()
    {
        var settings = new AppSettings();

        Assert.True(settings.ToggleFavouriteSpeaker("libritts", 9));
        Assert.True(settings.IsFavouriteSpeaker("libritts", 9));
        Assert.False(settings.IsFavouriteSpeaker("vctk", 9));

        Assert.False(settings.ToggleFavouriteSpeaker("libritts", 9));
        Assert.False(settings.IsFavouriteSpeaker("libritts", 9));
    }

    [Fact]
    public void UnstarringTheLastFavouriteDropsTheVoiceEntry()
    {
        var settings = new AppSettings();

        settings.ToggleFavouriteSpeaker("libritts", 9);
        settings.ToggleFavouriteSpeaker("libritts", 9);

        Assert.Empty(settings.FavouriteSpeakers);
    }

    [Fact]
    public void FavouritesKeepTheOrderTheyWereStarredIn()
    {
        var settings = new AppSettings();

        settings.ToggleFavouriteSpeaker("libritts", 40);
        settings.ToggleFavouriteSpeaker("libritts", 2);

        Assert.Equal(new[] { 40, 2 }, settings.GetFavouriteSpeakers("libritts"));
    }

    [Fact]
    public void SpeakerChoicesAndFavouritesSurviveASaveAndLoad()
    {
        var path = Path.Combine(_root, "settings.json");
        var settings = new AppSettings();

        settings.SetSpeakerId("libritts", 512);
        settings.ToggleFavouriteSpeaker("libritts", 512);
        settings.ToggleFavouriteSpeaker("libritts", 3);
        settings.SaveTo(path);

        var loaded = AppSettings.LoadFrom(path);

        Assert.Equal(512, loaded.GetSpeakerId("libritts"));
        Assert.Equal(new[] { 512, 3 }, loaded.GetFavouriteSpeakers("libritts"));
        Assert.True(loaded.IsFavouriteSpeaker("LIBRITTS", 3));
    }

    [Fact]
    public void ASettingsFileWrittenBeforeThisFeatureStillLoads()
    {
        var path = Path.Combine(_root, "old.json");
        File.WriteAllText(path, @"{ ""SpeakerId"": 5, ""Speed"": 1.2 }");

        var loaded = AppSettings.LoadFrom(path);

        Assert.Equal(5, loaded.GetSpeakerId("anything"));
        Assert.Empty(loaded.FavouriteSpeakers);
    }
}
