using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public class TextChunkerTests
{
    private static ChunkerOptions Options(int lineEnding = 200, int sentence = 0, int question = 0, int exclamation = 0) =>
        new()
        {
            MaxChunkLength = 200,
            Silence = new SilenceOptions
            {
                LineEndingMs = lineEnding,
                SentenceMs = sentence,
                QuestionMs = question,
                ExclamationMs = exclamation,
            },
        };

    [Fact]
    public void PlainTextBecomesOneUtterance()
    {
        var chunks = new TextChunker(Options()).Read("Hello there, friend.").ToList();

        Assert.Single(chunks);
        Assert.Equal("Hello there, friend.", chunks[0].Text);
        Assert.Equal(0, chunks[0].SilenceMs);
    }

    [Fact]
    public void LineFeedsSplitAndCarrySilence()
    {
        var chunks = new TextChunker(Options(lineEnding: 300)).Read("one\ntwo").ToList();

        Assert.Equal(2, chunks.Count);
        Assert.Equal("one", chunks[0].Text);
        Assert.Equal(300, chunks[0].SilenceMs);
        Assert.Equal("two", chunks[1].Text);
        Assert.Equal(0, chunks[1].SilenceMs);
    }

    [Fact]
    public void SentenceSilenceSplitsAndDropsTheDelimiter()
    {
        var chunks = new TextChunker(Options(lineEnding: 0, sentence: 500)).Read("One. Two.").ToList();

        Assert.Equal(2, chunks.Count);
        Assert.Equal("One", chunks[0].Text);
        Assert.Equal(500, chunks[0].SilenceMs);
        Assert.Equal(" Two", chunks[1].Text);
    }

    [Fact]
    public void RepeatedDelimitersProduceSilenceOnlyOnce()
    {
        var chunks = new TextChunker(Options(lineEnding: 0, question: 400)).Read("What???").ToList();

        Assert.Single(chunks);
        Assert.Equal("What", chunks[0].Text);
        Assert.Equal(400, chunks[0].SilenceMs);
    }

    [Fact]
    public void ZeroSilenceKeepsTheDelimiterInTheText()
    {
        var chunks = new TextChunker(Options(lineEnding: 0)).Read("One. Two!").ToList();

        Assert.Single(chunks);
        Assert.Equal("One. Two!", chunks[0].Text);
    }

    [Fact]
    public void SilenceIsScaledByTheSpeechRate()
    {
        var options = Options(lineEnding: 400);
        options.ScaleSilenceToRate = true;
        options.SpeechRate = 2.0f;

        var chunks = new TextChunker(options).Read("one\ntwo").ToList();

        Assert.Equal(200, chunks[0].SilenceMs);
    }

    [Fact]
    public void LongInputIsSplitNearTheChunkLimit()
    {
        var options = Options(lineEnding: 0);
        options.MaxChunkLength = 64;
        var text = string.Join(" ", Enumerable.Repeat("word", 60));

        var chunks = new TextChunker(options).Read(text).ToList();

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Text.Length <= 64));
        Assert.Equal(text.Replace(" ", string.Empty), string.Concat(chunks.Select(c => c.Text)).Replace(" ", string.Empty));
    }

    [Fact]
    public void CharacterCountsAreTracked()
    {
        var chunker = new TextChunker(Options(lineEnding: 100));
        var text = "alpha\nbeta";

        var chunks = chunker.Read(text).ToList();

        Assert.Equal(text.Length, chunker.CharactersRead);
        Assert.Equal(0, chunks[0].InputStartIndex);
        Assert.Equal(6, chunks[1].InputStartIndex);
    }

    [Fact]
    public void EmptyInputProducesNothing()
    {
        Assert.Empty(new TextChunker(Options()).Read(string.Empty).ToList());
    }
}
