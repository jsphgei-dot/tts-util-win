using TtsUtil.Core.Update;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class ChangelogTests
{
    private const string Markdown = @"# Changelog

Newest first.

## 0.7.0-beta (version code 11)

* **Lists that ship.** The Aliases tab now offers chemistry,
  units and everyday shorthand.
* A second press no longer runs the same command twice.

## 0.6.0-beta (version code 10)

* Several texts at once.
";

    [Fact]
    public void EachHeadingBecomesAReleaseInTheOrderItIsWritten()
    {
        var releases = Changelog.Parse(Markdown);

        Assert.Equal(
            new[] { "0.7.0-beta (version code 11)", "0.6.0-beta (version code 10)" },
            releases.Select(release => release.Title));
    }

    /// <summary>A bullet that wraps onto the next line is one note, with the stars taken off.</summary>
    [Fact]
    public void AWrappedBulletComesBackAsOneLine()
    {
        var newest = Changelog.Parse(Markdown)[0];

        Assert.Equal(2, newest.Notes.Count);
        Assert.Equal(
            "Lists that ship. The Aliases tab now offers chemistry, units and everyday shorthand.",
            newest.Notes[0]);
    }
}
