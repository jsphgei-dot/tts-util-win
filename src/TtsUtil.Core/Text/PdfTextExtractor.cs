/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace TtsUtil.Core.Text;

/// <summary>An image lifted off a page that carried no text layer.</summary>
public sealed class PdfPageImage
{
    public PdfPageImage(int pageNumber, byte[] bytes)
    {
        PageNumber = pageNumber;
        Bytes = bytes;
    }

    public int PageNumber { get; }

    public byte[] Bytes { get; }
}

/// <summary>Recognizes text in a page image. Implemented outside Core, which is not Windows bound.</summary>
public interface IPageImageOcr
{
    Task<string> RecogniseAsync(PdfPageImage image, CancellationToken cancellationToken = default);
}

/// <summary>What one page yielded, and where the text came from.</summary>
public sealed class PdfPageText
{
    public int Number { get; init; }

    public string Text { get; init; } = string.Empty;

    /// <summary>True when the page had no text layer and the text came from OCR, or nowhere.</summary>
    public bool IsScanned { get; init; }
}

public sealed class PdfExtractionResult
{
    public IReadOnlyList<PdfPageText> Pages { get; init; } = Array.Empty<PdfPageText>();

    /// <summary>Pages that had no text layer, whether or not OCR later filled them.</summary>
    public IReadOnlyList<int> ScannedPages => Pages.Where(p => p.IsScanned).Select(p => p.Number).ToList();

    /// <summary>Pages that ended up with nothing at all, so the user knows what was lost.</summary>
    public IReadOnlyList<int> EmptyPages =>
        Pages.Where(p => p.Text.Trim().Length == 0).Select(p => p.Number).ToList();

    public string Text => string.Join(Environment.NewLine + Environment.NewLine,
        Pages.Where(p => p.Text.Trim().Length > 0).Select(p => p.Text));
}

/// <summary>Reads the text layer of a PDF, falling back to OCR on pages that have none.</summary>
public sealed class PdfTextExtractor
{
    private readonly IPageImageOcr? _ocr;

    public PdfTextExtractor(IPageImageOcr? ocr = null) => _ocr = ocr;

    /// <summary>True when the file starts with a PDF header, rather than merely ending in .pdf.</summary>
    public static bool LooksLikePdf(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var header = new byte[5];
            return stream.Read(header, 0, header.Length) == header.Length &&
                   Encoding.ASCII.GetString(header) == "%PDF-";
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static bool HasPdfExtension(string path) =>
        string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase);

    /// <summary>Extracts every page, reporting progress as "page n of m".</summary>
    public async Task<PdfExtractionResult> ExtractAsync(
        string path,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var pages = new List<PdfPageText>();

        using var document = PdfDocument.Open(path);
        var total = document.NumberOfPages;

        for (var number = 1; number <= total; number++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Reading page {number} of {total}...");

            var page = document.GetPage(number);
            var text = ReadPageText(page);

            if (text.Trim().Length > 0)
            {
                pages.Add(new PdfPageText { Number = number, Text = text });
                continue;
            }

            var recognized = await RunOcrAsync(page, number, total, progress, cancellationToken);
            pages.Add(new PdfPageText { Number = number, Text = recognized, IsScanned = true });
        }

        return new PdfExtractionResult { Pages = pages };
    }

    /// <summary>Pulls the embedded images off a page, which is what a scan usually is.</summary>
    public static IReadOnlyList<PdfPageImage> ImagesOn(Page page)
    {
        var images = new List<PdfPageImage>();

        foreach (var image in page.GetImages())
        {
            if (image.TryGetPng(out var png) && png is not null)
            {
                images.Add(new PdfPageImage(page.Number, png));
                continue;
            }

            // Otherwise the raw stream, which for a scan is usually a JPEG that decodes fine.
            if (!image.RawMemory.IsEmpty) images.Add(new PdfPageImage(page.Number, image.RawMemory.ToArray()));
        }

        return images;
    }

    private static string ReadPageText(Page page)
    {
        // The layout aware extractor keeps reading order, which matters for columns.
        var text = ContentOrderTextExtractor.GetText(page);
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
    }

    private async Task<string> RunOcrAsync(
        Page page,
        int number,
        int total,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        if (_ocr is null) return string.Empty;

        var images = ImagesOn(page);
        if (images.Count == 0) return string.Empty;

        progress?.Report($"No text on page {number} of {total}, reading the image...");

        var builder = new StringBuilder();
        foreach (var image in images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var text = await _ocr.RecogniseAsync(image, cancellationToken);
                if (text.Trim().Length > 0) builder.AppendLine(text.Trim());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // One unreadable image should not lose the rest of the document.
            }
        }

        return builder.ToString();
    }
}
