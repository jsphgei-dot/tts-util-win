/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;

namespace TtsUtil.App;

/// <summary>Asks before a text tab with unsaved words is closed, and offers to stop asking.</summary>
public partial class CloseTabWindow : Window
{
    public CloseTabWindow(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        Loaded += (_, _) => CloseButton.Focus();
    }

    /// <summary>Whether the reader ticked the box that turns the question off.</summary>
    public bool StopAsking => StopAskingBox.IsChecked == true;

    private void OnClose(object sender, RoutedEventArgs e) => DialogResult = true;
}
