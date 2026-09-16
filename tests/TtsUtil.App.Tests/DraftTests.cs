using System.IO;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class DraftTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _settingsPath;

    public DraftTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinDraft", Guid.NewGuid().ToString("N"));
        _settingsPath = Path.Combine(_root, "settings.json");
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
    public void EveryTextTabComesBackAsItWasLeft()
    {
        var first = CreateWindow();

        _wpf.Invoke(() =>
        {
            first.InputText.Text = "Where I left off.";
            first.TextScriptTitle = "Act One";
            first.NewDocument().Box.Text = "The other one.";
            first.Close();
        });

        var second = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Equal(new[] { "Act One", "Text 1" }, second.Documents.Select(document => document.Title));
            Assert.Equal("Where I left off.", second.InputText.Text);
            Assert.Equal("Act One", second.TextScriptTitle);
            Assert.Equal("The other one.", second.Documents[1].Box.Text);
            second.Close();
        });
    }

    [Fact]
    public void ClosingOnAnEmptyTabLeavesNothingToComeBackTo()
    {
        var window = CreateWindow();
        _wpf.Invoke(window.Close);

        Assert.False(File.Exists(Path.Combine(_root, TextDraft.FileName)));
    }

    private MainWindow CreateWindow() => _wpf.Invoke(() =>
    {
        var settings = AppSettings.LoadFrom(_settingsPath);
        settings.VoicesDirectory = Path.Combine(_root, "voices");
        settings.UseWindowsVoices = false;

        return new MainWindow(settings, loadVoiceOnSelection: false);
    });
}
