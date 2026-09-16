using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.App.Tests;

[Collection("wpf")]
public sealed class EditorToolbarTests : IDisposable
{
    private readonly WpfFixture _wpf;
    private readonly string _root;
    private readonly string _scriptsDir;
    private readonly MainWindow _window;

    public EditorToolbarTests(WpfFixture wpf)
    {
        _wpf = wpf;
        _root = Path.Combine(Path.GetTempPath(), "TtsUtilWinEditor", Guid.NewGuid().ToString("N"));
        _scriptsDir = Path.Combine(_root, "scripts");
        Directory.CreateDirectory(Path.Combine(_root, "voices"));
        Directory.CreateDirectory(_scriptsDir);

        _window = _wpf.Invoke(() =>
        {
            var settings = AppSettings.LoadFrom(Path.Combine(_root, "settings.json"));
            settings.VoicesDirectory = Path.Combine(_root, "voices");
            settings.OutputDirectory = _root;

            return new MainWindow(settings, loadVoiceOnSelection: false)
            {
                Scripts = new ScriptLibrary(_scriptsDir),
            };
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
    public void TheTitleAboveTheTextSavesTheScriptUnderThatName()
    {
        _wpf.Invoke(() =>
        {
            _window.InputText.Text = "The body.";
            _window.TextTitleBox.Text = "Opening remarks";
        });

        Click(_window.SaveScriptFromTextButton);

        Assert.Equal("The body.", File.ReadAllText(Path.Combine(_scriptsDir, "Opening remarks.txt")));
    }

    [Fact]
    public void BothTitleBoxesShowTheSameName()
    {
        _wpf.Invoke(() => _window.TextTitleBox.Text = "Shared");

        _wpf.Invoke(() => Assert.Equal("Shared", _window.ScriptTitleBox.Text));
    }

    [Fact]
    public void ControlSSavesWithoutTheButton()
    {
        _wpf.Invoke(() =>
        {
            _window.InputText.Text = "Saved by the keyboard.";
            _window.TextTitleBox.Text = "Shortcut";
            _window.SaveScriptFromText();
        });

        Assert.Equal("Saved by the keyboard.", File.ReadAllText(Path.Combine(_scriptsDir, "Shortcut.txt")));
    }

    [Fact]
    public void AToolWithNothingSelectedWorksOnTheWholeText()
    {
        _wpf.Invoke(() => _window.InputText.Text = "One\nTwo");

        Click(_window.BulletsButton);

        _wpf.Invoke(() => Assert.Equal("• One\n• Two", _window.InputText.Text));
    }

    [Fact]
    public void AToolWithASelectionWorksOnWholeLinesOnly()
    {
        _wpf.Invoke(() =>
        {
            _window.InputText.Text = "One\nTwo\nThree";

            // Part way through the middle line, which should still be indented in full.
            _window.InputText.Select(5, 1);
        });

        Click(_window.IndentButton);

        _wpf.Invoke(() => Assert.Equal("One\n    Two\nThree", _window.InputText.Text));
    }

    [Fact]
    public void ReplaceAllReportsHowManyItChanged()
    {
        _wpf.Invoke(() =>
        {
            _window.InputText.Text = "cat cat";
            _window.FindBox.Text = "cat";
            _window.ReplaceBox.Text = "dog";
        });

        Click(_window.ReplaceAllButton);

        _wpf.Invoke(() =>
        {
            Assert.Equal("dog dog", _window.InputText.Text);
            Assert.Equal("Replaced 2 of cat.", _window.StatusHistory[^1]);
        });
    }

    [Fact]
    public void TheFindStripStartsHiddenAndTheButtonShowsIt()
    {
        _wpf.Invoke(() => Assert.Equal(Visibility.Collapsed, _window.FindPanel.Visibility));

        Click(_window.FindButton);

        _wpf.Invoke(() => Assert.Equal(Visibility.Visible, _window.FindPanel.Visibility));
    }

    [Fact]
    public void TheEditorSizeIsRememberedInSettings()
    {
        _wpf.Invoke(() => _window.EditorFontSizeBox.SelectedItem = 20d);

        _wpf.Invoke(() =>
        {
            Assert.Equal(20d, _window.InputText.FontSize);
            Assert.Equal(20d, _window.Settings.EditorFontSize);
        });
    }

    private void Click(ButtonBase button) =>
        _wpf.Invoke(() => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));
}
