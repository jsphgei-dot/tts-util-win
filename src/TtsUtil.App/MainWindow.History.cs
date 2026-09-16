/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Collections.ObjectModel;
using System.Windows;
using TtsUtil.Core.Text;

namespace TtsUtil.App;

/// <summary>The recent messages, one bordered row each, a page at a time and newest first.</summary>
public partial class MainWindow
{
    /// <summary>Kept when the setting holds nothing usable.</summary>
    internal const int StatusHistoryLimit = 200;

    /// <summary>Shown on one page when the setting holds nothing usable.</summary>
    internal const int StatusHistoryPageSize = 10;

    private readonly List<string> _statusHistory = new();
    private readonly ObservableCollection<string> _historyPage = new();
    private int _historyPageNumber = 1;

    internal IReadOnlyList<string> StatusHistory => _statusHistory;

    /// <summary>The rows on the page as shown, newest first.</summary>
    internal IReadOnlyList<string> HistoryPageRows => _historyPage;

    internal int HistoryPageNumber => _historyPageNumber;

    internal int HistoryPageCount => HistoryPaging.PageCount(_statusHistory.Count, PageSize);

    private int KeptMessages => _settings.KeepLastMessages > 0 ? _settings.KeepLastMessages : StatusHistoryLimit;

    private int PageSize => _settings.ShowLastMessages > 0 ? _settings.ShowLastMessages : StatusHistoryPageSize;

    /// <summary>Writes a message the way the program does, for tests that need a history.</summary>
    internal void SetStatusForTest(string message) => SetStatus(message);

    /// <summary>Adds a message to the history and drops the oldest past the limit.</summary>
    private void RecordStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        // Progress overwrites itself many times a second, and none of it is worth keeping.
        if (_statusHistory.Count > 0 && _statusHistory[^1] == message) return;

        _statusHistory.Add(message);
        while (_statusHistory.Count > KeptMessages) _statusHistory.RemoveAt(0);

        if (StatusHistoryPopup?.IsOpen == true) ShowHistoryPage(_historyPageNumber);
    }

    private void OnShowStatusHistory(object sender, RoutedEventArgs e)
    {
        ShowHistoryPage(1);
        StatusHistoryPopup.IsOpen = true;
    }

    private void OnHideStatusHistory(object sender, RoutedEventArgs e) => StatusHistoryPopup.IsOpen = false;

    /// <summary>Clicking away closes the popup, and the button must not stay pressed.</summary>
    private void OnStatusHistoryClosed(object? sender, EventArgs e) => StatusHistoryButton.IsChecked = false;

    /// <summary>One page further back in time.</summary>
    private void OnHistoryOlder(object sender, RoutedEventArgs e) => ShowHistoryPage(_historyPageNumber + 1);

    private void OnHistoryNewer(object sender, RoutedEventArgs e) => ShowHistoryPage(_historyPageNumber - 1);

    private void OnHistoryNewest(object sender, RoutedEventArgs e) => ShowHistoryPage(1);

    /// <summary>A typed page lands when Enter is pressed, and out of range numbers clamp.</summary>
    private void OnHistoryPageTyped(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;

        ShowHistoryPage(int.TryParse(HistoryPageBox.Text, out var typed) ? typed : _historyPageNumber);
        e.Handled = true;
    }

    private void OnCopyHistory(object sender, RoutedEventArgs e)
    {
        if (_statusHistory.Count == 0)
        {
            SetStatus("There is no history to copy yet.");
            return;
        }

        try
        {
            Clipboard.SetText(string.Join(Environment.NewLine, Enumerable.Reverse(_statusHistory)));
        }
        catch (Exception)
        {
            // Another program can hold the clipboard open, and losing a copy is not worth a dialog.
        }
    }

    private void ShowHistoryPage(int page)
    {
        if (HistoryRows is null) return;

        var newestFirst = Enumerable.Reverse(_statusHistory).ToList();
        var pageCount = HistoryPaging.PageCount(newestFirst.Count, PageSize);
        _historyPageNumber = HistoryPaging.ClampPage(page, pageCount);

        _historyPage.Clear();
        foreach (var row in HistoryPaging.Page(newestFirst, _historyPageNumber, PageSize)) _historyPage.Add(row);

        HistoryRows.ItemsSource = _historyPage;
        HistoryEmptyText.Visibility = _statusHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        HistoryPageBox.Text = _historyPageNumber.ToString();
        HistoryPageCountText.Text = "/ " + pageCount;

        // Disabled rather than hidden, so the strip keeps its width as it is used.
        HistoryOlderButton.IsEnabled = _historyPageNumber < pageCount;
        HistoryNewerButton.IsEnabled = _historyPageNumber > 1;
        HistoryNewestButton.IsEnabled = _historyPageNumber > 1;
    }

    private void OnHistoryPageBoxLost(object sender, RoutedEventArgs e) => HistoryPageBox.Text =
        _historyPageNumber.ToString();

    private void LoadHistorySettings()
    {
        KeepLastMessagesBox.Text = KeptMessages.ToString();
        ShowLastMessagesBox.Text = PageSize.ToString();
    }

    /// <summary>Reads the two numbers back out of Settings when Apply is pressed.</summary>
    private void ApplyHistorySettings()
    {
        if (int.TryParse(KeepLastMessagesBox.Text, out var keep) && keep > 0)
        {
            _settings.KeepLastMessages = Math.Min(keep, 5000);
        }

        if (int.TryParse(ShowLastMessagesBox.Text, out var show) && show > 0)
        {
            _settings.ShowLastMessages = Math.Min(show, 200);
        }

        while (_statusHistory.Count > KeptMessages) _statusHistory.RemoveAt(0);
        LoadHistorySettings();
    }
}
