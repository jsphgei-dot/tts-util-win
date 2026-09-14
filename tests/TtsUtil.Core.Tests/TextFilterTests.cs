using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.Core.Tests;

public class TextFilterTests
{
    private static string Apply(string text, TextFilterOptions options)
    {
        var buffer = text.ToList();
        TextFilters.Apply(buffer, options);
        return new string(buffer.ToArray());
    }

    [Fact]
    public void HashesAreRemovedWhenEnabled()
    {
        var result = Apply("# heading ## two", new TextFilterOptions { FilterHashes = true });

        Assert.Equal(" heading  two", result);
    }

    [Fact]
    public void WebLinksAreRemovedWhenEnabled()
    {
        var result = Apply("see https://example.com/page now", new TextFilterOptions { FilterWebLinks = true });

        Assert.Equal("see  now", result);
    }

    [Fact]
    public void MailToLinksAreRemovedWhenEnabled()
    {
        var result = Apply("write mailto:someone@example.com today", new TextFilterOptions { FilterMailToLinks = true });

        Assert.Equal("write  today", result);
    }

    [Fact]
    public void PlainTextIsUntouched()
    {
        var options = new TextFilterOptions { FilterHashes = true, FilterWebLinks = true, FilterMailToLinks = true };

        Assert.Equal("nothing to remove here", Apply("nothing to remove here", options));
    }

    [Fact]
    public void DisabledFiltersRemoveNothing()
    {
        var text = "# https://example.com mailto:a@b.com";

        Assert.Equal(text, Apply(text, new TextFilterOptions()));
    }

    [Fact]
    public void LinkAtTheEndOfInputIsRemoved()
    {
        var result = Apply("read https://example.com", new TextFilterOptions { FilterWebLinks = true });

        Assert.Equal("read ", result);
    }
}
