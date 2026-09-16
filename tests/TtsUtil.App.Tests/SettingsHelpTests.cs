using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class SettingsHelpTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly MainWindow _window;

    public SettingsHelpTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinHelp", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        _window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = _root;
            settings.UseWindowsVoices = false;
            return new MainWindow(settings, loadVoiceOnSelection: false);
        });
    }

    public void Dispose()
    {
        _wpf.Invoke(() => _window.Close());

        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void EveryQuestionMarkPointsAtAnExplanation()
    {
        var keys = _wpf.Invoke(() => HelpButtons().Select(button => button.Tag as string).ToList());

        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.NotNull(SettingsHelp.Find(key)));
    }

    [Fact]
    public void PressingOneShowsThatExplanation()
    {
        var expected = SettingsHelp.Find(SettingsHelp.AllowedExtra)!;

        _wpf.Invoke(() =>
        {
            var button = HelpButtons().Single(b => (string?)b.Tag == SettingsHelp.AllowedExtra);
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            Assert.Equal(expected.Title, _window.SettingsHelpTitle.Text);
            Assert.Equal(expected.Body, _window.SettingsHelpBody.Text);
            Assert.Same(button, _window.SettingsHelpPopup.PlacementTarget);
        });
    }

    /// <summary>The question marks that name a settings topic. The ones on the File and Voices
    /// tabs carry their own text instead, so they have no tag.</summary>
    private IEnumerable<Button> HelpButtons()
    {
        var style = (Style)_window.FindResource("HelpButton");
        return Descendants(_window).OfType<Button>()
            .Where(button => button.Style == style && button.Tag is not null);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;

            foreach (var further in Descendants(child)) yield return further;
        }
    }
}
