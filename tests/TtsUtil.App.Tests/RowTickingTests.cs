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

    /// <summary>A shift press carries the pressed row's new state across the run.</summary>
    [Fact]
    public void AShiftPressTicksEverythingBetween()
    {
        var rows = DownloadableVoices.All.Take(4).Select(v => new VoiceCatalogueRow(v)).ToList();

        Assert.True(RowTicking.ToggleRun(rows, 0, 2));
        Assert.Equal(new[] { true, true, true, false }, rows.Select(r => r.Ticked));

        Assert.True(RowTicking.ToggleRun(rows, 2, 1));
        Assert.Equal(new[] { true, false, false, false }, rows.Select(r => r.Ticked));

        Assert.False(RowTicking.ToggleRun(rows, -1, 0));
    }
}
