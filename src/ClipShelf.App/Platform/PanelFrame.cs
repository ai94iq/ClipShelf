using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;

namespace ClipShelf.App.Platform;

// The flyout's window chrome: borderless, topmost, out of the taskbar, rounded, frameless.
internal static class PanelFrame
{
    private const int WidthLogical = 360;
    private const int HeightLogical = 480;
    private const int MarginLogical = 12;

    public static void Configure(Window window)
    {
        // Configure the existing presenter; replacing it (SetPresenter) drops the SystemBackdrop
        // and the flyout would render opaque.
        if (window.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Out of the taskbar and Alt+Tab; strip the frame styles WinUI's window class adds
        // (WS_EX_WINDOWEDGE, WS_CAPTION, WS_THICKFRAME are the light 1px border).
        var exStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        exStyle = (exStyle | NativeMethods.WsExToolWindow) & ~NativeMethods.WsExWindowEdge & ~NativeMethods.WsExClientEdge;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, new IntPtr(exStyle));

        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlStyle).ToInt64();
        style &= ~(NativeMethods.WsCaption | NativeMethods.WsThickFrame | NativeMethods.WsBorder);
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlStyle, new IntPtr(style));

        NativeMethods.SetWindowPos(
            handle, IntPtr.Zero, 0, 0, 0, 0,
            NativeMethods.SwpFrameChanged | NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder);

        ApplyTheme(window);
    }

    // Anchor the flyout to the bottom-right of the monitor under the cursor.
    public static void PositionBottomRightOfCursor(Window window)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var dpi = NativeMethods.GetDpiForWindow(handle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var width = (int)(WidthLogical * scale);
        var height = (int)(HeightLogical * scale);
        var margin = (int)(MarginLogical * scale);

        NativeMethods.GetCursorPos(out var cursor);
        var monitor = NativeMethods.MonitorFromPoint(cursor, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.MonitorInfo { CbSize = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return;

        var bounds = new Windows.Graphics.RectInt32(
            info.Work.Right - margin - width,
            info.Work.Bottom - margin - height,
            width,
            height);
        var position = window.AppWindow.Position;
        var size = window.AppWindow.Size;
        if (position.X != bounds.X || position.Y != bounds.Y ||
            size.Width != bounds.Width || size.Height != bounds.Height)
            window.AppWindow.MoveAndResize(bounds);
    }

    public static void WatchActivation(Window window, Action onDeactivated)
    {
        window.Activated += (_, args) =>
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                onDeactivated();
                return;
            }

            ApplyTheme(window);
        };
    }

    // DWM restores the default frame when the window activates, so this runs again on activation.
    public static void ApplyTheme(Window window)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);

        var dark = Application.Current.RequestedTheme == ApplicationTheme.Dark ? 1 : 0;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmImmersiveDarkMode, ref dark, sizeof(int));

        var corner = NativeMethods.DwmWindowCornerRound;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowCornerPreference, ref corner, sizeof(int));

        var border = NativeMethods.DwmColorNone;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowBorderColor, ref border, sizeof(int));
    }
}
