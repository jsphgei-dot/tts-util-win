using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class SpeakerCatalogTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ttsutil-speakers-" + Guid.NewGuid().ToString("N"));

    public SpeakerCatalogTests() => Directory.CreateDirectory(_root);

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
    public void AVoiceWithoutNamesFallsBackToNumbers()
    {
        var speakers = SpeakerCatalog.Build(3);

        Assert.Equal(new[] { "0", "1", "2" }, speakers.Select(s => s.Label));
        Assert.All(speakers, s => Assert.Equal(SpeakerNameSource.Number, s.Source));
    }


    [Fact]
    public void ThePiperSpeakerMapIsInvertedIntoLabels()
    {
        const string json = @"{
          ""num_speakers"": 3,
          ""speaker_id_map"": { ""p225"": 0, ""p226"": 1, ""p227"": 2 }
        }";

        var names = SpeakerCatalog.ParseModelNames(json, 3);
        var speakers = SpeakerCatalog.Build(3, names);

        Assert.Equal(new[] { "0  p225", "1  p226", "2  p227" }, speakers.Select(s => s.Label));
        Assert.All(speakers, s => Assert.Equal(SpeakerNameSource.Model, s.Source));
    }

    [Fact]
    public void SpeakersBeyondTheModelCountAreIgnored()
    {
        const string json = @"{ ""speaker_id_map"": { ""kept"": 0, ""dropped"": 9 } }";

        var names = SpeakerCatalog.ParseModelNames(json, 2);

        Assert.Equal(new[] { 0 }, names.Keys);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{}")]
    [InlineData(@"{ ""speaker_id_map"": [] }")]
    [InlineData(@"{ ""speaker_id_map"": { ""odd"": ""one"" } }")]
    public void UnusableMetadataLeavesTheNumbersAlone(string json)
    {
        Assert.Empty(SpeakerCatalog.ParseModelNames(json, 4));
    }

    [Fact]
    public void ARealLibrittsMetadataFileNamesEverySpeaker()
    {
        var model = Path.Combine(_root, "libritts.onnx");
        var entries = string.Join(", ", Enumerable.Range(0, 904).Select(i => $"\"{4000 + i}\": {i}"));
        File.WriteAllText(model + ".json",
            "{ \"num_speakers\": 904, \"speaker_id_map\": { " + entries + " } }");
        File.WriteAllText(model, "not a real model");

        var voice = new VoiceDescriptor { Name = "libritts", Directory = _root, ModelPath = model };
        var speakers = SpeakerCatalog.Load(voice, 904);

        Assert.Equal(904, speakers.Count);
        Assert.Equal("0  4000", speakers[0].Label);
        Assert.Equal("903  4903", speakers[903].Label);
    }

    [Fact]
    public void BareLinesInASpeakerFileNumberThemselves()
    {
        var lines = new[] { "Narrator", "", "# a comment", "Alice", "Bob" };

        var names = SpeakerCatalog.ParseUserNames(lines, 5);

        Assert.Equal("Narrator", names[0]);
        Assert.Equal("Alice", names[1]);
        Assert.Equal("Bob", names[2]);
        Assert.Equal(3, names.Count);
    }

    [Fact]
    public void ExplicitIdsInASpeakerFileAreHonoured()
    {
        var lines = new[] { "42 = Alice", "  7=Bob  ", "900 = out of range" };

        var names = SpeakerCatalog.ParseUserNames(lines, 100);

        Assert.Equal("Alice", names[42]);
        Assert.Equal("Bob", names[7]);
        Assert.Equal(2, names.Count);
    }

    [Fact]
    public void AUserNameBeatsTheModelName()
    {
        var model = new Dictionary<int, string> { [0] = "p225", [1] = "p226" };
        var user = new Dictionary<int, string> { [1] = "The narrator" };

        var speakers = SpeakerCatalog.Build(2, model, user);

        Assert.Equal("0  p225", speakers[0].Label);
        Assert.Equal(SpeakerNameSource.Model, speakers[0].Source);
        Assert.Equal("1  The narrator", speakers[1].Label);
        Assert.Equal(SpeakerNameSource.User, speakers[1].Source);
    }

    [Fact]
    public void ASpeakerFileOnDiskIsReadBesideTheModel()
    {
        var model = Path.Combine(_root, "voice.onnx");
        File.WriteAllText(model, "not a real model");
        File.WriteAllLines(Path.Combine(_root, SpeakerCatalog.SpeakerFileName), new[] { "3 = Deep voice" });

        var voice = new VoiceDescriptor { Name = "voice", Directory = _root, ModelPath = model };
        var speakers = SpeakerCatalog.Load(voice, 4);

        Assert.Equal("3  Deep voice", speakers[3].Label);
        Assert.Equal("0", speakers[0].Label);
    }

    [Fact]
    public void AnEmptyQueryReturnsEverySpeaker()
    {
        var speakers = SpeakerCatalog.Build(5);

        Assert.Equal(5, SpeakerCatalog.Filter(speakers, null).Count);
        Assert.Equal(5, SpeakerCatalog.Filter(speakers, "   ").Count);
    }

    [Fact]
    public void ANameQueryIsCaseInsensitiveAndPartial()
    {
        var names = new Dictionary<int, string> { [0] = "Alice", [1] = "Bob", [2] = "Alicia" };
        var speakers = SpeakerCatalog.Build(3, names);

        var found = SpeakerCatalog.Filter(speakers, "ali");

        Assert.Equal(new[] { 0, 2 }, found.Select(s => s.Id));
    }

    [Fact]
    public void ANumberQueryMatchesTheIdByPrefix()
    {
        var speakers = SpeakerCatalog.Build(400);

        var found = SpeakerCatalog.Filter(speakers, "12");

        Assert.Contains(found, s => s.Id == 12);
        Assert.Contains(found, s => s.Id == 120);
        Assert.DoesNotContain(found, s => s.Id == 312);
    }

    [Fact]
    public void FavouritesComeFirstAndKeepTheirIdOrder()
    {
        var speakers = SpeakerCatalog.Build(6);

        var found = SpeakerCatalog.Filter(speakers, null, new[] { 4, 1 });

        Assert.Equal(new[] { 1, 4, 0, 2, 3, 5 }, found.Select(s => s.Id));
    }

    [Fact]
    public void AFavouriteThatDoesNotMatchTheQueryStaysHidden()
    {
        var names = new Dictionary<int, string> { [0] = "Alice", [1] = "Bob" };
        var speakers = SpeakerCatalog.Build(2, names);

        var found = SpeakerCatalog.Filter(speakers, "bob", new[] { 0 });

        Assert.Equal(new[] { 1 }, found.Select(s => s.Id));
    }
}
