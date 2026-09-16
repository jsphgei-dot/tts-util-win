/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using CheckBox = System.Windows.Controls.CheckBox;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace TtsUtil.App;

/// <summary>A row that carries a tick of its own.</summary>
public interface ITickable
{
    bool Ticked { get; set; }
}

/// <summary>Anywhere a list shows a tick per row, a press anywhere on the row moves it.</summary>
public static class RowTicking
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(RowTicking), new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject element, bool value) =>
        element.SetValue(EnabledProperty, value);

    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

    /// <summary>Moves a row's tick, and says whether the row had one to move.</summary>
    internal static bool Toggle(object? item)
    {
        if (item is not ITickable row) return false;

        row.Ticked = !row.Ticked;
        return true;
    }

    private static void OnEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not ItemsControl list) return;

        if ((bool)e.NewValue) list.PreviewMouseLeftButtonUp += OnRowPressed;
        else list.PreviewMouseLeftButtonUp -= OnRowPressed;
    }

    private static void OnRowPressed(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ItemsControl list || e.OriginalSource is not DependencyObject source) return;

        // A press on the tick or on a button in the row is that control's own business.
        if (Above<CheckBox>(source) is not null || Above<ButtonBase>(source) is not null) return;

        var container = list.ContainerFromElement(source) as FrameworkElement;
        Toggle(container?.DataContext);
    }

    private static T? Above<T>(DependencyObject? source)
        where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T found) return found;

            source = VisualTreeHelper.GetParent(source) ?? LogicalTreeHelper.GetParent(source);
        }

        return null;
    }
}
