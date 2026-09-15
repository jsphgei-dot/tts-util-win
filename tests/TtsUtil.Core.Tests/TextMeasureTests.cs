using System.Text;
using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class TextMeasureTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ttsutil-measure-" + Guid.NewGuid().ToString("N"));

    public TextMeasureTests() => Directory.CreateDirectory(_root);

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
    public void AnEmptyReaderCountsZero()
    {
        Assert.Equal(0, TextMeasure.CountCharacters(new StringReader(string.Empty)));
    }

    [Fact]
    public void AsciiCountsOnePerCharacter()
    {
        Assert.Equal(11, TextMeasure.CountCharacters(new StringReader("hello world")));
    }

    [Fact]
    public void TextLongerThanTheBufferIsCountedInFull()
    {
        var text = new string('x', 50000);

        Assert.Equal(50000, TextMeasure.CountCharacters(new StringReader(text)));
    }

    [Theory]
    [InlineData("café naïve")]
    [InlineData("über straße")]
    [InlineData("你好世界")]
    [InlineData("مرحبا بالعالم")]
    public void AccentedAndNonLatinTextCountsCharactersNotBytes(string text)
    {
        var path = Path.Combine(_root, "sample.txt");
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var bytes = new FileInfo(path).Length;
        var characters = TextMeasure.CountCharacters(() => new StreamReader(path, detectEncodingFromByteOrderMarks: true));

        Assert.Equal(text.Length, characters);
        Assert.True(bytes > characters, $"expected more bytes ({bytes}) than characters ({characters})");
    }

    [Fact]
    public void AUtf16FileCountsHalfItsBytes()
    {
        var path = Path.Combine(_root, "utf16.txt");
        const string text = "plain ascii text";
        File.WriteAllText(path, text, new UnicodeEncoding(bigEndian: false, byteOrderMark: true));

        var characters = TextMeasure.CountCharacters(() => new StreamReader(path, detectEncodingFromByteOrderMarks: true));

        Assert.Equal(text.Length, characters);
        Assert.Equal((text.Length + 1) * 2, new FileInfo(path).Length);
    }

    [Fact]
    public void TheFactoryOverloadLeavesTheCallersReaderAlone()
    {
        var path = Path.Combine(_root, "reuse.txt");
        File.WriteAllText(path, "abcdef");

        var count = TextMeasure.CountCharacters(() => new StreamReader(path));

        using var fresh = new StreamReader(path);
        Assert.Equal(6, count);
        Assert.Equal("abcdef", fresh.ReadToEnd());
    }
}
