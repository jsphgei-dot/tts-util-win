/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Threading;
using Mouse = System.Windows.Input.Mouse;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseEventHandler = System.Windows.Input.MouseEventHandler;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using SizeChangedEventArgs = System.Windows.SizeChangedEventArgs;
using TabControl = System.Windows.Controls.TabControl;
using TabItem = System.Windows.Controls.TabItem;

namespace TtsUtil.App;

/// <summary>Anywhere a tab strip is turned on, its tabs share the row evenly the way a browser's
/// do, and hold their width while the pointer is anywhere on that row.</summary>
public static class TabStripSizing
{
    /// <summary>The name the header scroller carries in the tab control's template.</summary>
    public const string StripName = "HeaderScroller";

    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(TabStripSizing), new PropertyMetadata(false, OnEnabledChanged));

    /// <summary>How wide one tab is allowed to get with the row to itself.</summary>
    public static readonly DependencyProperty WidestProperty = DependencyProperty.RegisterAttached(
        "Widest", typeof(double), typeof(TabStripSizing), new PropertyMetadata(180d));

    /// <summary>How narrow tabs go before the row starts scrolling instead.</summary>
    public static readonly DependencyProperty NarrowestProperty = DependencyProperty.RegisterAttached(
        "Narrowest", typeof(double), typeof(TabStripSizing), new PropertyMetadata(80d));

    /// <summary>Set on a tab that keeps its own width, a plus at the end of the row for one, so
    /// the room it takes is left out of the share.</summary>
    public static readonly DependencyProperty SharesProperty = DependencyProperty.RegisterAttached(
        "Shares", typeof(bool), typeof(TabStripSizing), new PropertyMetadata(true));

    public static void SetEnabled(DependencyObject element, bool value) =>
        element.SetValue(EnabledProperty, value);

    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

    public static void SetWidest(DependencyObject element, double value) =>
        element.SetValue(WidestProperty, value);

    public static double GetWidest(DependencyObject element) => (double)element.GetValue(WidestProperty);

    public static void SetNarrowest(DependencyObject element, double value) =>
        element.SetValue(NarrowestProperty, value);

    public static double GetNarrowest(DependencyObject element) =>
        (double)element.GetValue(NarrowestProperty);

    public static void SetShares(DependencyObject element, bool value) =>
        element.SetValue(SharesProperty, value);

    public static bool GetShares(DependencyObject element) => (bool)element.GetValue(SharesProperty);

    /// <summary>The window handler put in place for a strip, kept so it can be taken off again.</summary>
    private static readonly DependencyProperty WindowHookProperty = DependencyProperty.RegisterAttached(
        "WindowHook", typeof(MouseEventHandler), typeof(TabStripSizing), new PropertyMetadata(null));

    /// <summary>A sizing waiting on the pointer to leave the row.</summary>
    private static readonly DependencyProperty HeldProperty = DependencyProperty.RegisterAttached(
        "Held", typeof(bool), typeof(TabStripSizing), new PropertyMetadata(false));

    /// <summary>The width every tab takes when the row has this much room for them.</summary>
    public static double TabWidth(double room, int count, double widest, double narrowest) =>
        Math.Clamp(room / Math.Max(count, 1), narrowest, widest);

    /// <summary>Gives every sharing tab the same width. A pointer on the tab row holds the sizing,
    /// so closing several in a row does not move the next close button out from under it.</summary>
    public static void Size(TabControl tabs) => Size(tabs, holdForPointer: true);

    /// <summary>Sizes the tabs whatever the pointer is doing, which a tab that has just opened
    /// needs, as it would otherwise sit at the width of its own name until the pointer left.</summary>
    public static void SizeNow(TabControl tabs) => Size(tabs, holdForPointer: false);

    /// <summary>The sideways scroller the tab headers sit in, once the template has been built.</summary>
    public static ScrollViewer? Strip(TabControl tabs)
    {
        tabs.ApplyTemplate();
        return tabs.Template?.FindName(StripName, tabs) as ScrollViewer;
    }

    /// <summary>The hold is settled after the layout, so a tab that has gone leaves the row at its
    /// new width rather than at the width it had before.</summary>
    private static void Size(TabControl tabs, bool holdForPointer)
    {
        if (Strip(tabs) is not ScrollViewer strip) return;

        tabs.Dispatcher.BeginInvoke(new Action(() =>
        {
            var held = holdForPointer && PointerOnTabRow(strip);
            tabs.SetValue(HeldProperty, held);
            if (!held) ApplyTabWidth(tabs, strip);
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

    private static void ApplyTabWidth(TabControl tabs, ScrollViewer strip)
    {
        var sharing = tabs.Items.OfType<TabItem>().Where(GetShares).ToList();
        var kept = tabs.Items.OfType<TabItem>().Where(tab => !GetShares(tab)).Sum(tab => tab.ActualWidth);

        var room = strip.ViewportWidth - kept - 8;
        if (room <= 0 || sharing.Count == 0) return;

        var width = TabWidth(room, sharing.Count, GetWidest(tabs), GetNarrowest(tabs));
        foreach (var tab in sharing) tab.Width = width;
    }

    private static void OnEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not TabControl tabs) return;

        if ((bool)e.NewValue) Watch(tabs);
        else Unwatch(tabs);
    }

    private static void Watch(TabControl tabs)
    {
        tabs.SizeChanged += OnTabsResized;
        tabs.MouseMove += OnPointerMoved;
        tabs.MouseLeave += OnPointerMoved;
        tabs.Loaded += OnTabsLoaded;
    }

    private static void Unwatch(TabControl tabs)
    {
        tabs.SizeChanged -= OnTabsResized;
        tabs.MouseMove -= OnPointerMoved;
        tabs.MouseLeave -= OnPointerMoved;
        tabs.Loaded -= OnTabsLoaded;

        if (tabs.GetValue(WindowHookProperty) is not MouseEventHandler hook) return;

        if (Window.GetWindow(tabs) is Window window)
        {
            window.MouseMove -= hook;
            window.MouseLeave -= hook;
        }

        tabs.ClearValue(WindowHookProperty);
    }

    /// <summary>The window is watched as well, so a pointer that leaves the row for somewhere the
    /// tab control does not cover still works the held sizing out again.</summary>
    private static void OnTabsLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TabControl tabs) return;

        if (Window.GetWindow(tabs) is Window window && tabs.GetValue(WindowHookProperty) is null)
        {
            MouseEventHandler hook = (_, _) => ReleaseHold(tabs);
            tabs.SetValue(WindowHookProperty, hook);
            window.MouseMove += hook;
            window.MouseLeave += hook;
        }

        Size(tabs);
    }

    private static void OnTabsResized(object sender, SizeChangedEventArgs e)
    {
        if (sender is TabControl tabs) Size(tabs);
    }

    private static void OnPointerMoved(object sender, MouseEventArgs e)
    {
        if (sender is TabControl tabs) ReleaseHold(tabs);
    }

    /// <summary>A pointer that has left the row is what works a held sizing out again, wherever in
    /// the window it went.</summary>
    private static void ReleaseHold(TabControl tabs)
    {
        if (!(bool)tabs.GetValue(HeldProperty)) return;

        if (Strip(tabs) is ScrollViewer strip && !PointerOnTabRow(strip)) SizeNow(tabs);
    }
}
