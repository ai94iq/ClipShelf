using System.Runtime.InteropServices;
using ClipShelf.App.Platform;
using ClipShelf.App.Shell;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace ClipShelf.App.Features.ClipboardPanel;

// A borderless, always-on-top flyout at the bottom-right of the active monitor, styled like the
// Windows 11 clipboard flyout. It hides when it loses focus or the user presses Esc.
public sealed partial class ClipboardPanelWindow : Window
{
    private const int WidthLogical = 360;
    private const int HeightLogical = 480;
    private const int MarginLogical = 12;

    private readonly SettingsService _settings;
    private readonly Lazy<AppShellService> _shell;
    private IntPtr _previousWindow = IntPtr.Zero;

    public ClipboardPanelWindow(
        ClipboardPanelViewModel viewModel,
        SettingsService settings,
        Lazy<AppShellService> shell)
    {
        ViewModel = viewModel;
        _settings = settings;
        _shell = shell;

        InitializeComponent();
        Root.DataContext = viewModel;
        Title = Tr.Get("App_Name");
        this.ApplyCultureDirection();
        DestructiveHover.TintOnHover(ClearAllButton);
        DestructiveHover.TintOnHover(ConfirmClearButton);

        PanelFrame.Configure(this);
        WindowSurface.ApplyAcrylic(this, Root);
        Activated += OnActivated;
        ViewModel.ItemActivated += OnItemActivated;
        ViewModel.OpenHistoryRequested += OnOpenHistoryRequested;
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
        Reposition();
        AppWindow.Show();
        Activate();
        PanelFrame.ApplyTheme(this);
        SearchBox.Focus(FocusState.Programmatic);
    }

    public void Hide() => AppWindow.Hide();

    // Anchors the flyout to the bottom-right of the monitor under the cursor.
    private void Reposition()
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
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            Hide();
            return;
        }

        PanelFrame.ApplyTheme(this);
    }

    private void OnItemActivated(object? sender, EventArgs e)
    {
        Hide();
        if (_settings.Current.PasteOnSelect) _ = KeyboardPaste.IntoAsync(_previousWindow);
    }

    private void OnOpenHistoryRequested(object? sender, EventArgs e)
    {
        Hide();
        _shell.Value.ShowHistory();
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ClipItemViewModel item) ViewModel.Activate(item);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape) return;

        // Esc cancels the confirmation first, and only then closes the flyout.
        if (ViewModel.IsConfirmingClear)
            ViewModel.CancelClearCommand.Execute(null);
        else
            Hide();

        e.Handled = true;
    }
}
