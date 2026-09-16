using TtsUtil.Core.Settings;
using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class RowTickingTests
{
    /// <summary>A press anywhere on a row moves its tick, on any list whose rows carry one.</summary>
    [Fact]
    public void APressOnARowMovesItsTick()
    {
        var script = new ScriptRow(new SavedScript { Title = "One" });
        var voice = new VoiceCatalogueRow(DownloadableVoices.All[0]);

        Assert.True(RowTicking.Toggle(script));
        Assert.True(script.Chosen);

        Assert.True(RowTicking.Toggle(voice));
        Assert.True(voice.Ticked);

        Assert.True(RowTicking.Toggle(voice));
        Assert.False(voice.Ticked);

        Assert.False(RowTicking.Toggle("a row with no tick"));
    }
}
