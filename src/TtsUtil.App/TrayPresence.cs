/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.App;

/// <summary>The icon the window hides behind, among the hidden icons in the notification area.
/// It carries a menu for coming back and for quitting outright.</summary>
internal sealed class TrayPresence : IDisposable
{
    private readonly NotifyIcon _icon;

    internal TrayPresence(string title, Action restore, Action quit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => restore());
        menu.Items.Add("Quit", null, (_, _) => quit());

        _icon = new NotifyIcon
        {
            Text = title,
            Icon = ProgramIcon(),
            ContextMenuStrip = menu,
        };

        _icon.DoubleClick += (_, _) => restore();
    }

    internal void Show(string message)
    {
        _icon.Visible = true;
        _icon.ShowBalloonTip(3000, "TTS Util Win", message, ToolTipIcon.Info);
    }

    internal void Hide() => _icon.Visible = false;

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    /// <summary>The program's own icon where Windows will hand it over, or the shell default.</summary>
    private static Icon ProgramIcon()
    {
        var path = Environment.ProcessPath;
        return (path is null ? null : Icon.ExtractAssociatedIcon(path)) ?? SystemIcons.Application;
    }
}
