/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using MouseWheelEventHandler = System.Windows.Input.MouseWheelEventHandler;

namespace TtsUtil.App;

/// <summary>Hands the wheel on to whatever is behind a list, a grid or a text box once that
/// control has nothing left to scroll, so a pointer resting on one never traps the page.</summary>
internal static class WheelScrolling
{
    private static bool _registered;

    /// <summary>Applies to every scroller in the program, present and future.</summary>
    internal static void Enable()
    {
        if (_registered) return;

        _registered = true;
        EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.MouseWheelEvent,
            new MouseWheelEventHandler(OnWheel), handledEventsToo: true);
    }

    /// <summary>Whether a scroller has run out of room in the direction being asked for, which
    /// includes having nothing to scroll at all.</summary>
    internal static bool OutOfRoom(double offset, double scrollable, int delta) =>
        delta > 0 ? offset <= 0 : offset >= scrollable;

    private static void OnWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer view || e.Delta == 0) return;
        if (!OutOfRoom(view.VerticalOffset, view.ScrollableHeight, e.Delta)) return;
        if (VisualTreeHelper.GetParent(view) is not UIElement parent) return;

        e.Handled = true;
        parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = view,
        });
    }
}
