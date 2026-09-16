using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class AliasReaderTests
{
    [Fact]
    public void TextComesThroughWithTheAliasesApplied()
    {
        var reader = AliasTextReader.Wrap(new StringReader("the SQL server"), Aliases());

        Assert.Equal("the sequel server", reader.ReadToEnd());
    }

    [Fact]
    public void LineEndingsSurviveTheRules()
    {
        var reader = AliasTextReader.Wrap(new StringReader("SQL\r\nSQL\n"), Aliases());

        Assert.Equal("sequel\r\nsequel\n", reader.ReadToEnd());
    }

    [Fact]
    public void AnEmptyListIsNotWrappedAtAll()
    {
        var inner = new StringReader("unchanged");

        Assert.Same(inner, AliasTextReader.Wrap(inner, new AliasDictionary()));
    }

    private static AliasDictionary Aliases() => new()
    {
        Rules = { new AliasRule { Match = "SQL", SayAs = "sequel" } },
    };
}
