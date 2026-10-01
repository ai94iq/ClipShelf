using System.Runtime.InteropServices;
using ClipShelf.App.Platform;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace ClipShelf.App.Features.ClipboardPanel;

// A borderless, always-on-top flyout at the bottom-right of the active monitor, styled like the
// Windows 11 clipboard flyout: acrylic surface, rounded corners, no window frame, and a height
// that fits the content. It hides when it loses focus or the user presses Esc.
public sealed partial class ClipboardPanelWindow : Window
{
    private const int WidthLogical = 400;
    private const int MinHeightLogical = 200;
    private const int MaxHeightLogical = 560;
    private const int SearchRowLogical = 62;
    private const int FooterLogical = 46;
    private const int ItemLogical = 56;
    private const int VisibleRowsLogical = 8;
    private const int MarginLogical = 12;
    private const int PasteDelayMs = 60;

    private readonly SettingsService _settings;
    private IntPtr _previousWindow = IntPtr.Zero;

    public ClipboardPanelWindow(ClipboardPanelViewModel viewModel, SettingsService settings)
    {
        ViewModel = viewModel;
        _settings = settings;

        InitializeComponent();
        Root.DataContext = viewModel;
        Title = Tr.Get("App_Name");
        this.ApplyCultureDirection();

        ConfigureChrome();
        ConfigureSurface();
        Activated += OnActivated;
        ViewModel.ItemActivated += OnItemActivated;
        ViewModel.Items.CollectionChanged += (_, _) =>
        {
            if (AppWindow.IsVisible) FitToContent();
        };
    }

    public ClipboardPanelViewModel ViewModel { get; }

    public async Task ToggleAsync()
    {
        if (AppWindow.IsVisible)
        {
            Hide();
            return;
        }

        await ShowAsync();
    }

    public async Task ShowAsync()
    {
        _previousWindow = NativeMethods.GetForegroundWindow();
        await ViewModel.RefreshAsync();
        FitToContent();
        AppWindow.Show();
        Activate();
        ApplyFrame();   // activating can restore the default frame, so re-apply it
        SearchBox.Focus(FocusState.Programmatic);
    }

    public void Hide() => AppWindow.Hide();

    private void ConfigureChrome()
    {
        // Configure the window's existing presenter; replacing it (SetPresenter) drops the
        // SystemBackdrop and the flyout renders opaque.
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        // Keep the flyout out of the taskbar and Alt+Tab, and strip every window frame style
        // (WinUI's default window class adds WS_EX_WINDOWEDGE, WS_CAPTION and WS_THICKFRAME,
        // which is the light 1px border).
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var exStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        exStyle = (exStyle | NativeMethods.WsExToolWindow) & ~NativeMethods.WsExWindowEdge & ~NativeMethods.WsExClientEdge;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, new IntPtr(exStyle));

        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlStyle).ToInt64();
        style &= ~(NativeMethods.WsCaption | NativeMethods.WsThickFrame | NativeMethods.WsBorder);
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlStyle, new IntPtr(style));

        NativeMethods.SetWindowPos(
            handle, IntPtr.Zero, 0, 0, 0, 0,
            NativeMethods.SwpFrameChanged | NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder);

        ApplyFrame();
    }

    // Matches the frame to the app theme, rounds the corners, and removes the 1px border.
    private void ApplyFrame()
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);

        var dark = Application.Current.RequestedTheme == ApplicationTheme.Dark ? 1 : 0;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmImmersiveDarkMode, ref dark, sizeof(int));

        var corner = NativeMethods.DwmWindowCornerRound;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowCornerPreference, ref corner, sizeof(int));

        var border = NativeMethods.DwmColorNone;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowBorderColor, ref border, sizeof(int));
    }

    // Acrylic surface like the Windows 11 / PowerToys flyouts; solid where acrylic is unavailable.
    private void ConfigureSurface()
    {
        if (DesktopAcrylicController.IsSupported())
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
            Root.Background = null;
            return;
        }

        Root.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
    }

    private void FitToContent()
    {
        var count = ViewModel.Items.Count;
        var height = count == 0
            ? MinHeightLogical
            : SearchRowLogical + Math.Min(count, VisibleRowsLogical) * ItemLogical + FooterLogical;

        Reposition(Math.Clamp(height, MinHeightLogical, MaxHeightLogical));
    }

    // Anchors the flyout to the bottom-right of the monitor under the cursor.
    private void Reposition(int heightLogical)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var dpi = NativeMethods.GetDpiForWindow(handle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var width = (int)(WidthLogical * scale);
        var height = (int)(heightLogical * scale);
        var margin = (int)(MarginLogical * scale);

        NativeMethods.GetCursorPos(out var cursor);
        var monitor = NativeMethods.MonitorFromPoint(cursor, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.MonitorInfo { CbSize = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return;

        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            info.Work.Right - margin - width,
            info.Work.Bottom - margin - height,
            width,
            height));
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            Hide();
            return;
        }

        ApplyFrame();
    }

    private void OnItemActivated(object? sender, EventArgs e)
    {
        Hide();
        if (_settings.Current.PasteOnSelect) _ = PasteAsync();
    }

    private async Task PasteAsync()
    {
        if (_previousWindow == IntPtr.Zero) return;

        NativeMethods.SetForegroundWindow(_previousWindow);
        await Task.Delay(PasteDelayMs);

        NativeMethods.keybd_event(NativeMethods.VirtualKeyControl, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VirtualKeyV, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VirtualKeyV, 0, NativeMethods.KeyEventKeyUp, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VirtualKeyControl, 0, NativeMethods.KeyEventKeyUp, UIntPtr.Zero);
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ClipItemViewModel item) ViewModel.Activate(item);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape) return;

        Hide();
        e.Handled = true;
    }
}
