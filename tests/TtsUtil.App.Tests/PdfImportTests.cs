using System.IO;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class PdfImportTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _voicesDir;
    private readonly string _settingsPath;
    private readonly List<MainWindow> _windows = new();

    public PdfImportTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinPdf", Guid.NewGuid().ToString("N"));
        _voicesDir = Path.Combine(_root, "voices");
        _settingsPath = Path.Combine(_root, "settings.json");
        Directory.CreateDirectory(_voicesDir);
    }

    public void Dispose()
    {
        _wpf.Invoke(() =>
        {
            foreach (var window in _windows) window.Close();
        });

        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task ImportingAPdfPutsItsTextInTheTextTab()
    {
        var path = WritePdf("The opening line.", "The closing line.");
        var window = CreateWindow();

        _wpf.Invoke(() => window.FilePathBox.Text = path);
        await ClickAndSettle(window.ImportTextButton, window);

        _wpf.Invoke(() =>
        {
            Assert.Contains("The opening line", window.InputText.Text);
            Assert.Contains("The closing line", window.InputText.Text);
            Assert.Equal(0, window.Tabs.SelectedIndex);
            Assert.Contains("2 page(s)", window.FileInfoText.Text);
        });
    }

    [Fact]
    public async Task APdfWithNoTextSaysSoRatherThanReadingSilence()
    {
        var path = WritePdf();
        var window = CreateWindow();

        _wpf.Invoke(() => window.FilePathBox.Text = path);
        await ClickAndSettle(window.ImportTextButton, window);

        _wpf.Invoke(() =>
        {
            Assert.Contains("no text", window.StatusText.Text);
            Assert.Equal(string.Empty, window.InputText.Text);
        });
    }

    [Fact]
    public async Task APlainTextFileStillImportsThroughTheSameButton()
    {
        var path = Path.Combine(_root, "notes.txt");
        File.WriteAllText(path, "Just ordinary text.");
        var window = CreateWindow();

        _wpf.Invoke(() => window.FilePathBox.Text = path);
        await ClickAndSettle(window.ImportTextButton, window);

        _wpf.Invoke(() => Assert.Equal("Just ordinary text.", window.InputText.Text));
    }

    /// <summary>One page per line of text, or a single empty page when given none.</summary>
    private string WritePdf(params string[] lines)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        if (lines.Length == 0)
        {
            builder.AddPage(PageSize.A4);
        }
        else
        {
            foreach (var line in lines)
            {
                builder.AddPage(PageSize.A4).AddText(line, 12, new PdfPoint(30, 700), font);
            }
        }

        var path = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, builder.Build());
        return path;
    }

    /// <summary>Clicks, then awaits the extraction the click started.</summary>
    private async Task ClickAndSettle(ButtonBase button, MainWindow window)
    {
        var work = _wpf.Invoke(() =>
        {
            button.RaiseEvent(new System.Windows.RoutedEventArgs(ButtonBase.ClickEvent));
            return window.PendingWork;
        });

        await work;
    }

    private MainWindow CreateWindow()
    {
        var window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(_settingsPath);
            settings.VoicesDirectory = _voicesDir;
            settings.OutputDirectory = _root;

            return new MainWindow(settings, loadVoiceOnSelection: false);
        });

        _windows.Add(window);
        return window;
    }
}
