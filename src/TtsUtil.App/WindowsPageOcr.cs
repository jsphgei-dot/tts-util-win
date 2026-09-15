/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using TtsUtil.Core.Text;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace TtsUtil.App;

/// <summary>Reads text off a page image with the OCR engine built into Windows.</summary>
public sealed class WindowsPageOcr : IPageImageOcr
{
    /// <summary>Below this an image is a logo or a rule, not a scanned page.</summary>
    private const uint SmallestUsefulSide = 64;

    /// <summary>False when no language pack on this machine can be used for OCR.</summary>
    public static bool IsAvailable
    {
        get
        {
            try
            {
                return OcrEngine.TryCreateFromUserProfileLanguages() is not null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public async Task<string> RecogniseAsync(PdfPageImage image, CancellationToken cancellationToken = default)
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null || image.Bytes.Length == 0) return string.Empty;

        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new InMemoryRandomAccessStream();
        var writer = new DataWriter(stream);
        writer.WriteBytes(image.Bytes);
        await writer.StoreAsync();
        await writer.FlushAsync();
        writer.DetachStream();
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        if (decoder.PixelWidth < SmallestUsefulSide || decoder.PixelHeight < SmallestUsefulSide)
        {
            return string.Empty;
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        var result = await engine.RecognizeAsync(bitmap);
        return result.Text ?? string.Empty;
    }
}
