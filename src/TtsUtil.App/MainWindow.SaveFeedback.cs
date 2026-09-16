/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using Color = System.Windows.Media.Color;

namespace TtsUtil.App;

/// <summary>What the save icon does once a script has been written, and the guard that keeps a
/// second press from running the same command twice.</summary>
public partial class MainWindow
{
    internal static readonly Brush SavedNewBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x8B, 0x4C));

    internal static readonly Brush SavedOverBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB));

    private const string SaveGlyph = "";

    private const string SavedGlyph = "";

    private static readonly TimeSpan SaveFlashTime = TimeSpan.FromSeconds(1.6);

    private DispatcherTimer? _saveFlash;

    private bool _handlingClick;

    /// <summary>A tick on the save icon, green the first time a name is written and blue when an
    /// existing script is replaced.</summary>
    internal void ShowSaveConfirmation(bool replaced)
    {
        SaveScriptFromTextButton.Content = SavedGlyph;
        SaveScriptFromTextButton.Foreground = replaced ? SavedOverBrush : SavedNewBrush;

        if (_saveFlash is null)
        {
            _saveFlash = new DispatcherTimer { Interval = SaveFlashTime };
            _saveFlash.Tick += (_, _) => ClearSaveConfirmation();
        }

        _saveFlash.Stop();
        _saveFlash.Start();
    }

    /// <summary>Puts the icon back to how it looks when nothing has just been saved.</summary>
    internal void ClearSaveConfirmation()
    {
        _saveFlash?.Stop();
        SaveScriptFromTextButton.Content = SaveGlyph;
        SaveScriptFromTextButton.ClearValue(ForegroundProperty);
    }

    /// <summary>Takes the press when nothing else is being dealt with. A second press landing while
    /// the first is still being run is dropped.</summary>
    internal bool ClaimClick()
    {
        if (_handlingClick) return false;

        _handlingClick = true;
        Dispatcher.BeginInvoke(new Action(ReleaseClick), DispatcherPriority.ApplicationIdle);
        return true;
    }

    /// <summary>Lets the next press through.</summary>
    internal void ReleaseClick() => _handlingClick = false;

    private void OnPreviewButtonPress(object sender, MouseButtonEventArgs e)
    {
        if (ButtonAbove(e.OriginalSource as DependencyObject) is null) return;
        if (!ClaimClick()) e.Handled = true;
    }

    /// <summary>The button a press landed inside, which is rarely the element under the pointer.</summary>
    private static ButtonBase? ButtonAbove(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ButtonBase button) return button;
            source = VisualTreeHelper.GetParent(source) ?? LogicalTreeHelper.GetParent(source);
        }

        return null;
    }
}
