using System.IO;
using System.Windows;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class WindowsVoiceTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;

    public WindowsVoiceTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinWinVoices", Guid.NewGuid().ToString("N"));
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
    public void WindowsVoicesAreListedBesideTheDownloadedOnes()
    {
        var window = CreateWindow(useWindows: true);

        _wpf.Invoke(() => Assert.Equal(new[] { "David  (Windows, en-GB)" }, window.VoiceBox.Items.Cast<string>()));

        Close(window);
    }

    [Fact]
    public void TurningTheSettingOffTakesThemOutOfTheList()
    {
        var window = CreateWindow(useWindows: true);

        _wpf.Invoke(() =>
        {
            window.UseWindowsVoicesBox.IsChecked = false;
            window.UseWindowsVoicesBox.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

            Assert.Empty(window.VoiceBox.Items);
            Assert.False(window.Settings.UseWindowsVoices);
        });

        Close(window);
    }

    [Fact]
    public void ASherpaVoiceKeepsItsOwnLabel()
    {
        var sherpa = new VoiceDescriptor { Name = "piper-en", Kind = VoiceModelKind.Vits };

        Assert.Equal("piper-en  (Vits)", MainWindow.VoiceLabel(sherpa));
    }

    [Fact]
    public void AWindowsVoiceOnThisMachineSpeaksAndProducesSamples()
    {
        var installed = WindowsVoices.Scan();
        Assert.NotEmpty(installed);

        using var engine = WindowsTtsEngine.Load(installed[0]);
        Assert.NotNull(engine);

        var samples = 0;
        engine!.Synthesize("Testing one two three.", 0, 1.0f, chunk =>
        {
            samples += chunk.Length;
            return true;
        }, CancellationToken.None);

        Assert.True(samples > 0, "the voice produced no audio");
        Assert.True(engine.SampleRate > 0);
        Assert.Equal(1, engine.SpeakerCount);
    }

    private MainWindow CreateWindow(bool useWindows) => _wpf.Invoke(() =>
    {
        var settings = AppSettings.LoadFrom(Path.Combine(_root, Guid.NewGuid().ToString("N") + ".json"));
        settings.VoicesDirectory = Path.Combine(_root, "voices");
        settings.UseWindowsVoices = useWindows;

        return new MainWindow(settings, loadVoiceOnSelection: false)
        {
            WindowsVoiceScanner = () => new[]
            {
                new VoiceDescriptor { Name = "David", Source = VoiceSource.Windows, Id = "david", Language = "en-GB" },
            },
        };
    });

    private void Close(MainWindow window) => _wpf.Invoke(window.Close);
}
