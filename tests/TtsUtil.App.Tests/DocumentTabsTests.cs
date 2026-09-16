using System.IO;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class DocumentTabsTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;

    public DocumentTabsTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinDocuments", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "voices"));
    }

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
    public void EachTabKeepsItsOwnWordsAndScript()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.InputText.Text = "The first one.";
            window.TextScriptTitle = "Act One";

            var second = window.NewDocument();
            window.DocumentTabs.SelectedItem = second.Tab;
            window.InputText.Text = "The second one.";

            Assert.Null(window.TextScriptTitle);
            Assert.Equal("The first one.", window.Documents[0].Box.Text);
            Assert.Equal("The second one.", window.InputText.Text);

            window.Close();
        });
    }

    [Fact]
    public void ClosingTheLastTabEmptiesItInstead()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.InputText.Text = "Something to be rid of.";
            window.TextScriptTitle = "Act One";
            window.CloseDocument(window.ActiveDocument!);

            Assert.Single(window.Documents);
            Assert.Equal(string.Empty, window.InputText.Text);
            Assert.Null(window.TextScriptTitle);

            window.Close();
        });
    }

    private MainWindow CreateWindow() => _wpf.Invoke(() =>
    {
        var settings = AppSettings.LoadFrom(Path.Combine(_root, Guid.NewGuid().ToString("N") + ".json"));
        settings.VoicesDirectory = Path.Combine(_root, "voices");
        settings.UseWindowsVoices = false;

        return new MainWindow(settings, loadVoiceOnSelection: false);
    });
}
