/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Input;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using ComboBox = System.Windows.Controls.ComboBox;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using ListBoxItem = System.Windows.Controls.ListBoxItem;
using PasswordBox = System.Windows.Controls.PasswordBox;
using Slider = System.Windows.Controls.Slider;
using TabItem = System.Windows.Controls.TabItem;
using TextBoxBase = System.Windows.Controls.Primitives.TextBoxBase;

namespace TtsUtil.App;

/// <summary>Where the cursor goes when a press lands on the window itself.</summary>
public partial class MainWindow
{
    /// <summary>Takes the cursor out of a text box when the press landed on nothing that wants it,
    /// so no caret is left blinking in a box that has been clicked away from.</summary>
    internal void ReleaseTypingFocus(DependencyObject? source)
    {
        if (Keyboard.FocusedElement is not TextBoxBase) return;
        if (TakesFocusItself(source)) return;

        Keyboard.ClearFocus();
        FocusManager.SetFocusedElement(this, null);
    }

    /// <summary>Whether the press landed inside a control that settles the cursor on its own.</summary>
    private static bool TakesFocusItself(DependencyObject? source)
    {
        for (var node = source; node is not null; node = ParentOf(node))
        {
            if (node is TextBoxBase or ButtonBase or ComboBox or ComboBoxItem or ListBoxItem
                or Slider or TabItem or PasswordBox)
            {
                return true;
            }
        }

        return false;
    }
}
