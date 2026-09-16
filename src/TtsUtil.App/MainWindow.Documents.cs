/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using SystemColors = System.Windows.SystemColors;
using TextBox = System.Windows.Controls.TextBox;

namespace TtsUtil.App;

/// <summary>The Text tab holds several documents, one tab each, so audio can be written from one
/// while another is being written or read.</summary>
public partial class MainWindow
{
    private readonly List<TextDocument> _documents = new();
    private TabItem? _plusTab;

    /// <summary>The open documents, in tab order.</summary>
    internal IReadOnlyList<TextDocument> Documents => _documents;

    /// <summary>The document the user is looking at, or null before the window is built.</summary>
    internal TextDocument? ActiveDocument => (DocumentTabs?.SelectedItem as TabItem)?.Tag as TextDocument;

    /// <summary>The text box of the open document, which the rest of the window edits.</summary>
    internal TextBox InputText => ActiveDocument?.Box!;

    /// <summary>The script the open document holds, which names audio written from it.</summary>
    private string? ActiveScriptTitle
    {
        get => ActiveDocument?.ScriptTitle;
        set
        {
            var document = ActiveDocument;
            if (document is null) return;

            document.ScriptTitle = value;
            if (value is not null) Rename(document, value);
        }
    }

    /// <summary>Adds a document and gives it the editor's current size and wrapping.</summary>
    internal TextDocument NewDocument(string? title = null, string text = "", string? scriptTitle = null)
    {
        var box = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = WrapTextBox?.IsChecked == false ? TextWrapping.NoWrap : TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = WrapTextBox?.IsChecked == false
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled,
            FontSize = EditorFontSizeBox?.SelectedItem as double? ?? EditorFontSizes[1],
            Padding = new Thickness(6),
            Text = text,
        };
        box.TextChanged += OnInputTextChanged;

        var tab = new TabItem { Content = box };
        var document = new TextDocument(tab, box, title ?? NextDocumentTitle()) { ScriptTitle = scriptTitle };
        tab.Tag = document;
        tab.Header = BuildTabHeader(document);

        EnsurePlusTab();
        _documents.Add(document);
        DocumentTabs.Items.Insert(DocumentTabs.Items.Count - 1, tab);

        if (DocumentTabs.SelectedItem is null || ReferenceEquals(DocumentTabs.SelectedItem, _plusTab))
        {
            DocumentTabs.SelectedItem = tab;
        }

        return document;
    }

    /// <summary>Closes a document. The last one is emptied rather than taken away.</summary>
    internal void CloseDocument(TextDocument document)
    {
        if (_busy && ReferenceEquals(document, _runDocument))
        {
            SetStatus("Press Stop before closing this tab.");
            return;
        }

        CancelWriteFor(document);

        if (_documents.Count == 1)
        {
            document.Box.Clear();
            document.ScriptTitle = null;
            Rename(document, "Text 1");
            return;
        }

        var index = _documents.IndexOf(document);
        _documents.Remove(document);

        // Picking the neighbour first keeps the plus from being selected, and opening a tab.
        DocumentTabs.SelectedItem = _documents[Math.Min(index, _documents.Count - 1)].Tab;
        DocumentTabs.Items.Remove(document.Tab);
    }

    private void OnNewDocument(object sender, RoutedEventArgs e)
    {
        var document = NewDocument();
        DocumentTabs.SelectedItem = document.Tab;
        document.Box.Focus();
    }

    /// <summary>The line list and the script name follow whichever document is in front.</summary>
    private void OnDocumentTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, DocumentTabs)) return;

        if (ReferenceEquals(DocumentTabs.SelectedItem, _plusTab) && _plusTab is not null)
        {
            var added = NewDocument();
            DocumentTabs.SelectedItem = added.Tab;
            added.Box.Focus();
            return;
        }

        if (_initialising || InputText is null) return;

        _spokenLine = -1;
        RebuildLineList();
        if (ActiveScriptTitle is string title) ScriptTitleBox.Text = title;
    }

    /// <summary>A box being spoken or written is held still, and looks it. It can still be
    /// scrolled and copied from.</summary>
    private static void LockBox(TextBox box, bool locked)
    {
        box.IsReadOnly = locked;
        box.Background = locked ? SystemColors.ControlBrush : SystemColors.WindowBrush;
        box.Foreground = locked ? SystemColors.GrayTextBrush : SystemColors.WindowTextBrush;
    }

    /// <summary>The plus sits at the end of the strip, and opens a tab when it is picked.</summary>
    private void EnsurePlusTab()
    {
        if (_plusTab is not null) return;

        _plusTab = new TabItem
        {
            Header = new TextBlock { Text = "+", FontWeight = FontWeights.Bold, Margin = new Thickness(4, 0, 4, 0) },
            ToolTip = "Another text tab, written to audio alongside this one",
        };

        DocumentTabs.Items.Add(_plusTab);
    }

    private void Rename(TextDocument document, string title)
    {
        document.Title = title;
        document.Label.Text = title;
    }

    /// <summary>Numbers a new document past the ones already open.</summary>
    private string NextDocumentTitle()
    {
        for (var number = 1; ; number++)
        {
            var title = "Text " + number;
            if (!_documents.Any(document => document.Title == title)) return title;
        }
    }

    private object BuildTabHeader(TextDocument document)
    {
        var close = new Button
        {
            Content = "×",
            Style = (Style)FindResource("TabCloseButton"),
            Focusable = false,
            ToolTip = "Close this text tab",
        };
        close.Click += (_, _) => CloseDocument(document);
        DockPanel.SetDock(close, Dock.Right);

        var panel = new DockPanel();
        panel.Children.Add(close);
        panel.Children.Add(document.Label);
        return panel;
    }
}

/// <summary>One text document, its tab and the box the words are typed into.</summary>
internal sealed class TextDocument
{
    public TextDocument(TabItem tab, TextBox box, string title)
    {
        Tab = tab;
        Box = box;
        Title = title;
        Label = new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center };
    }

    public TabItem Tab { get; }

    public TextBox Box { get; }

    public TextBlock Label { get; }

    public string Title { get; set; }

    /// <summary>The script this document came from, which names audio written from it.</summary>
    public string? ScriptTitle { get; set; }
}
