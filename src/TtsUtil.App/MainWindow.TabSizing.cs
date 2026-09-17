/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows.Threading;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using SizeChangedEventArgs = System.Windows.SizeChangedEventArgs;

namespace TtsUtil.App;

/// <summary>Text tabs share the strip evenly the way a browser's do, and hold their size while the
/// pointer is among them.</summary>
public partial class MainWindow
{
    /// <summary>How wide one tab is allowed to get with the strip to itself.</summary>
    internal const double WidestTab = 180;

    /// <summary>How narrow tabs go before the strip starts scrolling instead.</summary>
    internal const double NarrowestTab = 80;

    private bool _pointerOnTabs;

    /// <summary>The width every tab takes when the strip has this much room for them.</summary>
    internal static double TabWidth(double room, int count) =>
        Math.Clamp(room / Math.Max(count, 1), NarrowestTab, WidestTab);

    /// <summary>Gives every open tab the same width. A pointer among the tabs holds the sizing, so
    /// closing several in a row does not move the next close button out from under it.</summary>
    internal void SizeTabs()
    {
        if (_pointerOnTabs || TabStrip() is not ScrollViewer strip) return;

        Dispatcher.BeginInvoke(new Action(() => ApplyTabWidth(strip)), DispatcherPriority.Loaded);
    }

    private void ApplyTabWidth(ScrollViewer strip)
    {
        var room = strip.ViewportWidth - (_plusTab?.ActualWidth ?? 0) - 8;
        if (room <= 0) return;

        var width = TabWidth(room, _documents.Count);
        foreach (var document in _documents) document.Tab.Width = width;
    }

    private void WatchTabStrip(ScrollViewer strip)
    {
        strip.PreviewMouseWheel += OnTabStripWheel;
        strip.SizeChanged += OnTabStripResized;
        strip.MouseEnter += OnTabStripEntered;
        strip.MouseLeave += OnTabStripLeft;
    }

    private void OnTabStripResized(object sender, SizeChangedEventArgs e) => SizeTabs();

    private void OnTabStripEntered(object sender, MouseEventArgs e) => _pointerOnTabs = true;

    /// <summary>The pointer leaving is when a held sizing is worked out again.</summary>
    private void OnTabStripLeft(object sender, MouseEventArgs e)
    {
        _pointerOnTabs = false;
        SizeTabs();
    }
}
