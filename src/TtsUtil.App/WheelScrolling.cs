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

        var outer = OuterScrollerWithRoom(view, e.Delta);
        if (outer is null) return;

        // The outer one is moved itself rather than sent another wheel event, which would come
        // straight back here.
        e.Handled = true;
        outer.ScrollToVerticalOffset(outer.VerticalOffset - e.Delta);
    }

    /// <summary>The nearest scroller further out that can still move the way the wheel is asking,
    /// or null when the page is already at its end.</summary>
    private static ScrollViewer? OuterScrollerWithRoom(DependencyObject view, int delta)
    {
        for (var node = VisualTreeHelper.GetParent(view); node is not null;
             node = VisualTreeHelper.GetParent(node))
        {
            if (node is ScrollViewer outer &&
                !OutOfRoom(outer.VerticalOffset, outer.ScrollableHeight, delta))
            {
                return outer;
            }
        }

        return null;
    }
}
