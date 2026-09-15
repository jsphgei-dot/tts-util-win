using TtsUtil.Core.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class PdfTextExtractorTests : IDisposable
{
    /// <summary>A 120 by 120 PNG, large enough that the extractor treats it as page content.</summary>
    private static readonly string PngBase64 = string.Concat(
        "iVBORw0KGgoAAAANSUhEUgAAAHgAAAB4CAIAAAC2BqGFAAABz0lEQVR4nO3QgUYeAAAA4V+SJEmSJEmSJEmSJEmSJE",
        "mSmSSZJEmSJEmSJEmSJEmSJEkymSSZSTJJkiRJkiRJkiTpMeK6Jzj3BQJBwSGhYeERkVHRMbFx8QmJSckpqWnpGZlZ",
        "2Tm5efkFhUXFJaVl5RWVVdU1P37W1tU3/Gpsam5pbWvv6Ozq7unt6x8YHBoeGR0bn5icmp6ZnZtfWFxaXlldW/+98W",
        "dza3vn77/dvf3/B4dHxyenZ+cXl1fXN7d39w+PT88vr2/vH9+lH/guo1/dF1poVl9ooVl9oYVm9YUWmtUXWmhWX2ih",
        "WX2hhWb1hRaa1RdaaFZfaKFZfaGFZvWFFprVF1poVl9ooVl9oYVm9YUWmtUXWmhWX2ihWX2hhWb1hRaa1RdaaFZfaK",
        "FZfaGFZvWFFprVF1poVl9ooVl9oYVm9YUWmtUXWmhWX2ihWX2hhWb1hRaa1RdaaFZfaKFZfaGFZvWFFprVF1poVl9o",
        "oVl9oYVm9YUWmtUXWmhWX2ihWX2hhWb1hRaa1RdaaFZfaKFZfaGFZvWFFprVF1poVl9ooVl9oYVm9YUWmtUXWmhWX2",
        "ihWX2hhWb1hRaa1RdaaFZfaKFZfaGFzkb1PwFpSiniH8lOlQAAAABJRU5ErkJggg==");

    private readonly string _root = Path.Combine(Path.GetTempPath(), "ttsutil-pdf-" + Guid.NewGuid().ToString("N"));

    public PdfTextExtractorTests() => Directory.CreateDirectory(_root);

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
    public async Task EveryPageOfTheTextLayerIsReadInOrder()
    {
        var path = WritePdf(("The first page.", false), ("The second page.", false), ("The third page.", false));

        var result = await new PdfTextExtractor().ExtractAsync(path);

        Assert.Equal(3, result.Pages.Count);
        Assert.Empty(result.ScannedPages);
        Assert.Contains("first", result.Text);
        Assert.True(result.Text.IndexOf("first", StringComparison.Ordinal) <
                    result.Text.IndexOf("third", StringComparison.Ordinal),
            "pages must be joined in reading order");
    }

    [Fact]
    public async Task APageOfImagesWithNoTextIsReportedAsScanned()
    {
        var path = WritePdf(("Real text here.", false), (null, true));

        var result = await new PdfTextExtractor().ExtractAsync(path);

        Assert.Equal(new[] { 2 }, result.ScannedPages);
        Assert.Equal(new[] { 2 }, result.EmptyPages);
        Assert.Contains("Real text", result.Text);
    }

    [Fact]
    public async Task OcrFillsInThePagesThatHaveNoTextLayer()
    {
        var path = WritePdf(("Page one text.", false), (null, true));
        var ocr = new FakeOcr("recognised words");

        var result = await new PdfTextExtractor(ocr).ExtractAsync(path);

        Assert.Equal(1, ocr.Calls);
        Assert.Contains("Page one text", result.Text);
        Assert.Contains("recognised words", result.Text);
        Assert.Empty(result.EmptyPages);
    }

    [Fact]
    public async Task OcrIsNotRunOnPagesThatAlreadyHaveText()
    {
        var path = WritePdf(("All pages.", false), ("Have text.", false));
        var ocr = new FakeOcr("should never appear");

        var result = await new PdfTextExtractor(ocr).ExtractAsync(path);

        Assert.Equal(0, ocr.Calls);
        Assert.DoesNotContain("should never appear", result.Text);
    }

    [Fact]
    public async Task AFailureReadingOnePageImageDoesNotLoseTheRest()
    {
        var path = WritePdf((null, true), ("Still readable.", false));
        var ocr = new FakeOcr("unused") { Throw = true };

        var result = await new PdfTextExtractor(ocr).ExtractAsync(path);

        Assert.Contains("Still readable", result.Text);
        Assert.Equal(new[] { 1 }, result.EmptyPages);
    }

    [Fact]
    public async Task CancellingStopsBeforeTheDocumentIsFinished()
    {
        var path = WritePdf(("One.", false), ("Two.", false), ("Three.", false));
        using var cancellation = new CancellationTokenSource();

        // Reporting inline on the extraction thread, so the cancel lands mid document.
        var progress = new InlineProgress(_ => cancellation.Cancel());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new PdfTextExtractor().ExtractAsync(path, progress, cancellation.Token));
    }

    [Fact]
    public void AFileIsJudgedByItsHeaderNotItsName()
    {
        var pretender = Path.Combine(_root, "not-really.pdf");
        File.WriteAllText(pretender, "This is plain text.");
        var real = WritePdf(("Real.", false));

        Assert.False(PdfTextExtractor.LooksLikePdf(pretender));
        Assert.True(PdfTextExtractor.LooksLikePdf(real));
    }

    /// <summary>Writes a PDF where each page carries either a line of text or an image.</summary>
    private string WritePdf(params (string? Text, bool Image)[] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var png = Convert.FromBase64String(PngBase64);

        foreach (var (text, image) in pages)
        {
            var page = builder.AddPage(PageSize.A4);
            if (text is not null) page.AddText(text, 12, new PdfPoint(30, 700), font);
            if (image) page.AddPng(png, new PdfRectangle(30, 300, 330, 600));
        }

        var path = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, builder.Build());
        return path;
    }

    private sealed class InlineProgress : IProgress<string>
    {
        private readonly Action<string> _report;

        public InlineProgress(Action<string> report) => _report = report;

        public void Report(string value) => _report(value);
    }

    private sealed class FakeOcr : IPageImageOcr
    {
        private readonly string _text;

        public FakeOcr(string text) => _text = text;

        public int Calls { get; private set; }

        public bool Throw { get; init; }

        public Task<string> RecogniseAsync(PdfPageImage image, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Throw) throw new InvalidOperationException("no engine");
            return Task.FromResult(_text);
        }
    }
}
