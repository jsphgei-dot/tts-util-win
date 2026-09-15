using System.IO;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Text;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class SpokenCharacterSettingsTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _voicesDir;
    private readonly string _settingsPath;
    private readonly List<MainWindow> _windows = new();

    public SpokenCharacterSettingsTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinSpoken", Guid.NewGuid().ToString("N"));
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
    public void TheStrictPolicyIsPreselectedForANewSettingsFile()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Equal(3, window.SpokenCharactersBox.Items.Count);
            Assert.Equal(0, window.SpokenCharactersBox.SelectedIndex);
            Assert.Equal(SpokenCharacterPolicy.LatinOnly, window.Settings.SpokenCharacters);
        });
    }

    [Fact]
    public void TheStoredPolicyIsPreselected()
    {
        var window = CreateWindow(s => s.SpokenCharacters = SpokenCharacterPolicy.Off);

        _wpf.Invoke(() => Assert.Equal(2, window.SpokenCharactersBox.SelectedIndex));
    }

    [Fact]
    public void ApplyStoresThePolicyAndTheExtraCharacters()
    {
        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            window.SpokenCharactersBox.SelectedIndex = 1;
            window.AllowedExtraBox.Text = "%$";
        });

        Click(window.ApplySettingsButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal(SpokenCharacterPolicy.AnyLetter, window.Settings.SpokenCharacters);
            Assert.Equal("%$", window.Settings.AllowedExtraCharacters);
        });

        var reloaded = AppSettings.LoadFrom(_settingsPath);
        Assert.Equal(SpokenCharacterPolicy.AnyLetter, reloaded.SpokenCharacters);
        Assert.Equal("%$", reloaded.AllowedExtraCharacters);
    }

    [Fact]
    public void ThePolicyIsStoredByNameSoTheFileStaysReadable()
    {
        var window = CreateWindow();

        _wpf.Invoke(() => window.SpokenCharactersBox.SelectedIndex = 1);
        Click(window.ApplySettingsButton);

        Assert.Contains("\"AnyLetter\"", File.ReadAllText(_settingsPath));
    }

    [Fact]
    public void ASettingsFileWithAnUnknownPolicyFallsBackToStrict()
    {
        File.WriteAllText(_settingsPath, @"{ ""SpokenCharacters"": ""Nonsense"" }");

        var window = CreateWindow();

        _wpf.Invoke(() =>
        {
            Assert.Equal(SpokenCharacterPolicy.LatinOnly, window.Settings.SpokenCharacters);
            Assert.Equal(0, window.SpokenCharactersBox.SelectedIndex);
        });
    }

    private void Click(ButtonBase button) =>
        _wpf.Invoke(() => button.RaiseEvent(new System.Windows.RoutedEventArgs(ButtonBase.ClickEvent)));

    private MainWindow CreateWindow(Action<AppSettings>? configure = null)
    {
        var window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(_settingsPath);
            settings.VoicesDirectory = _voicesDir;
            settings.OutputDirectory = _root;
            configure?.Invoke(settings);

            return new MainWindow(settings, loadVoiceOnSelection: false);
        });

        _windows.Add(window);
        return window;
    }
}
