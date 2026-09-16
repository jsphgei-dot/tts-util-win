/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Runtime.InteropServices;
using Windows.Media;

namespace TtsUtil.App;

/// <summary>What the transport controls are showing about the reading.</summary>
internal enum MediaState
{
    Closed,
    Playing,
    Paused,
    Stopped,
}

/// <summary>The Windows transport controls: the media keys on a keyboard, and the media
/// overlay. A desktop window asks for them by handle, through the interop below.</summary>
internal sealed class MediaControls : IDisposable
{
    private static readonly Guid ControlsIid = new("99fa3ff4-1742-42a6-902e-087d41f965ec");

    private readonly SystemMediaTransportControls _controls;

    private MediaControls(SystemMediaTransportControls controls)
    {
        _controls = controls;
        _controls.ButtonPressed += OnButtonPressed;
    }

    /// <summary>Raised on the pressed button, from a background thread.</summary>
    internal event Action<SystemMediaTransportControlsButton>? ButtonPressed;

    /// <summary>Attaches to a window, or returns null when Windows will not give them out.</summary>
    internal static MediaControls? ForWindow(IntPtr windowHandle)
    {
        try
        {
            var controls = GetForWindow(windowHandle);
            controls.IsEnabled = true;
            controls.IsPlayEnabled = true;
            controls.IsPauseEnabled = true;
            controls.IsStopEnabled = true;
            controls.PlaybackStatus = MediaPlaybackStatus.Closed;

            var display = controls.DisplayUpdater;
            display.Type = MediaPlaybackType.Music;
            display.MusicProperties.Title = "TTS Util Win";
            display.Update();

            return new MediaControls(controls);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Tells the overlay what the reading is doing.</summary>
    internal void Show(MediaState state)
    {
        try
        {
            _controls.PlaybackStatus = state switch
            {
                MediaState.Playing => MediaPlaybackStatus.Playing,
                MediaState.Paused => MediaPlaybackStatus.Paused,
                MediaState.Stopped => MediaPlaybackStatus.Stopped,
                _ => MediaPlaybackStatus.Closed,
            };
        }
        catch (Exception)
        {
            // The overlay is a courtesy. Losing it is not worth interrupting a reading for.
        }
    }

    /// <summary>Names what is being read, which the overlay shows beside the buttons.</summary>
    internal void Describe(string title)
    {
        try
        {
            var display = _controls.DisplayUpdater;
            display.Type = MediaPlaybackType.Music;
            display.MusicProperties.Title = string.IsNullOrWhiteSpace(title) ? "TTS Util Win" : title;
            display.MusicProperties.Artist = "TTS Util Win";
            display.Update();
        }
        catch (Exception)
        {
        }
    }

    public void Dispose()
    {
        try
        {
            _controls.ButtonPressed -= OnButtonPressed;
            _controls.IsEnabled = false;
        }
        catch (Exception)
        {
        }
    }

    private void OnButtonPressed(SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args) => ButtonPressed?.Invoke(args.Button);

    private static SystemMediaTransportControls GetForWindow(IntPtr windowHandle)
    {
        var name = "Windows.Media.SystemMediaTransportControls";
        var hr = WindowsCreateString(name, name.Length, out var classId);
        Marshal.ThrowExceptionForHR(hr);

        try
        {
            var factoryIid = typeof(ISystemMediaTransportControlsInterop).GUID;
            hr = RoGetActivationFactory(classId, ref factoryIid, out var factory);
            Marshal.ThrowExceptionForHR(hr);

            var interop = (ISystemMediaTransportControlsInterop)Marshal.GetObjectForIUnknown(factory);
            Marshal.Release(factory);

            var iid = ControlsIid;
            var abi = interop.GetForWindow(windowHandle, ref iid);
            return WinRT.MarshalInspectable<SystemMediaTransportControls>.FromAbi(abi);
        }
        finally
        {
            WindowsDeleteString(classId);
        }
    }

    [DllImport("combase.dll", CharSet = CharSet.Unicode)]
    private static extern int WindowsCreateString(string source, int length, out IntPtr result);

    [DllImport("combase.dll")]
    private static extern int WindowsDeleteString(IntPtr value);

    [DllImport("combase.dll")]
    private static extern int RoGetActivationFactory(IntPtr classId, ref Guid iid, out IntPtr factory);

    /// <summary>The three IInspectable slots come first, so GetForWindow lands on the right one.</summary>
    [ComImport]
    [Guid("ddb0472d-c911-4a1f-86d9-dc3d71a95f5a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISystemMediaTransportControlsInterop
    {
        void GetIids(out int count, out IntPtr iids);

        void GetRuntimeClassName(out IntPtr className);

        void GetTrustLevel(out int trustLevel);

        IntPtr GetForWindow(IntPtr window, ref Guid iid);
    }
}
