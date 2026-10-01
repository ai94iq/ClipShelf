using System.Runtime.InteropServices;
using ClipShelf.App.Platform;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace ClipShelf.App.Features.ClipboardPanel;

// A borderless, always-on-top flyout at the bottom-right of the active monitor. It hides when it
// loses focus or the user presses Esc, like the Windows clipboard flyout.
public sealed partial class ClipboardPanelWindow : Window
{
    private const int WidthLogical = 400;
    private const int HeightLogical = 520;
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
        Position();
        AppWindow.Show();
        Activate();
        SearchBox.Focus(FocusState.Programmatic);
    }

    public void Hide() => AppWindow.Hide();

    private void ConfigureChrome()
    {
        var presenter = OverlappedPresenter.Create();
        presenter.IsAlwaysOnTop = true;
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.SetBorderAndTitleBar(false, false);
        AppWindow.SetPresenter(presenter);

        // Keep the flyout out of the taskbar and Alt+Tab.
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, new IntPtr(style | NativeMethods.WsExToolWindow));

        // Rounded corners like the PowerToys tray flyouts; ignored on Windows 10.
        var corner = NativeMethods.DwmWindowCornerRound;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowCornerPreference, ref corner, sizeof(int));
    }

    // Acrylic surface like the PowerToys tray flyouts; solid where acrylic is unavailable.
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

    private void Position()
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var dpi = NativeMethods.GetDpiForWindow(handle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var width = (int)(WidthLogical * scale);
        var height = (int)(HeightLogical * scale);
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
        if (args.WindowActivationState == WindowActivationState.Deactivated) Hide();
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
