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

    /// <summary>Eight tabs stay on one line, so closing one cannot reshuffle the rows.</summary>
    [Fact]
    public void TheTabStripIsOneRowThatScrollsSideways()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            for (var more = 0; more < 7; more++) window.NewDocument();

            window.DocumentTabs.ApplyTemplate();
            var strip = window.DocumentTabs.Template.FindName("HeaderScroller", window.DocumentTabs);
            var scroller = Assert.IsType<System.Windows.Controls.ScrollViewer>(strip);
            var panel = Assert.IsType<System.Windows.Controls.StackPanel>(scroller.Content);

            Assert.Equal(System.Windows.Controls.Orientation.Horizontal, panel.Orientation);
            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Disabled, scroller.VerticalScrollBarVisibility);

            window.CloseDocument(window.Documents[3]);
            Assert.Equal(7, window.Documents.Count);

            window.Close();
        });
    }

    [Fact]
    public void ClosingTheLastTabEmptiesItInstead()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.UnsavedCloseAsker = _ => (Close: true, StopAsking: false);
            window.InputText.Text = "Something to be rid of.";
            window.TextScriptTitle = "Act One";
            window.CloseDocument(window.ActiveDocument!);

            Assert.Single(window.Documents);
            Assert.Equal(string.Empty, window.InputText.Text);
            Assert.Null(window.TextScriptTitle);

            window.Close();
        });
    }

    [Fact]
    public void PickingThePlusOpensATabAndBringsItForward()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.DocumentTabs.SelectedItem = window.DocumentTabs.Items[window.DocumentTabs.Items.Count - 1];

            Assert.Equal(2, window.Documents.Count);
            Assert.Same(window.Documents[1], window.ActiveDocument);

            window.CloseDocument(window.Documents[1]);
            Assert.Single(window.Documents);
            Assert.Same(window.Documents[0], window.ActiveDocument);

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
