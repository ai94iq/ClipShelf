using Microsoft.UI.Windowing;

namespace ClipShelf.App.Platform;

// The flyout's window chrome: borderless, topmost, out of the taskbar, rounded, frameless.
internal static class PanelFrame
{
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
