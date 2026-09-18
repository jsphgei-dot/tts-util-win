/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows.Threading;
using Mouse = System.Windows.Input.Mouse;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using SizeChangedEventArgs = System.Windows.SizeChangedEventArgs;

namespace TtsUtil.App;

/// <summary>Text tabs share the strip evenly the way a browser's do, and hold their size while the
/// pointer is anywhere on the row they sit in.</summary>
public partial class MainWindow
{
    /// <summary>How wide one tab is allowed to get with the strip to itself.</summary>
    internal const double WidestTab = 180;

    /// <summary>How narrow tabs go before the strip starts scrolling instead.</summary>
    internal const double NarrowestTab = 80;

    private bool _sizingHeld;

    /// <summary>The width every tab takes when the strip has this much room for them.</summary>
    internal static double TabWidth(double room, int count) =>
        Math.Clamp(room / Math.Max(count, 1), NarrowestTab, WidestTab);

    /// <summary>Gives every open tab the same width. A pointer on the tab row holds the sizing, so
    /// closing several in a row does not move the next close button out from under it.</summary>
    internal void SizeTabs() => SizeTabs(holdForPointer: true);

    /// <summary>Sizes the tabs whatever the pointer is doing, which a tab that has just opened
    /// needs, as it would otherwise sit at the width of its own name until the pointer left.</summary>
    internal void SizeTabsNow() => SizeTabs(holdForPointer: false);

    /// <summary>The hold is settled after the layout, so a tab that has gone takes the pointer off
    /// the tabs at once rather than at the next thing the pointer does.</summary>
    private void SizeTabs(bool holdForPointer)
    {
        if (TabStrip() is not ScrollViewer strip) return;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            _sizingHeld = holdForPointer && PointerOnTabRow(strip);
            if (!_sizingHeld) ApplyTabWidth(strip);
        }), DispatcherPriority.Loaded);
    }

    /// <summary>Whether the pointer is in the row the tabs sit in, the whole box from the left of
    /// the first tab to the far end of the row, not the tabs alone.</summary>
    private static bool PointerOnTabRow(ScrollViewer strip)
    {
        if (!strip.IsVisible) return false;

        var pointer = Mouse.GetPosition(strip);
        return pointer.X >= 0 && pointer.X <= strip.ActualWidth &&
               pointer.Y >= 0 && pointer.Y <= strip.ActualHeight;
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
        MouseMove += OnPointerMoved;
        MouseLeave += OnPointerMoved;
    }

    private void OnTabStripResized(object sender, SizeChangedEventArgs e) => SizeTabs();

    /// <summary>A pointer that has left the row is what works a held sizing out again, wherever in
    /// the window it went.</summary>
    private void OnPointerMoved(object sender, MouseEventArgs e)
    {
        if (!_sizingHeld || TabStrip() is not ScrollViewer strip) return;
        if (!PointerOnTabRow(strip)) SizeTabsNow();
    }
}
