using ClipShelf.App.Platform;
using ClipShelf.App.Shell;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace ClipShelf.App.Features.Settings;

public sealed partial class SettingsPage : UserControl
{
    private readonly Lazy<AppShellService> _shell;
    private bool _recordingHotkey;

    public SettingsPage(SettingsViewModel viewModel, Lazy<AppShellService> shell)
    {
        ViewModel = viewModel;
        _shell = shell;
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }

    private void OnExit(object sender, RoutedEventArgs e) => _shell.Value.Exit();

    private void OnHotkeyClick(object sender, RoutedEventArgs e)
    {
        _recordingHotkey = true;
        HotkeyButton.Content = Tr.Get("Settings_HotkeyPrompt");
        HotkeyButton.Focus(FocusState.Programmatic);
    }

    private void OnHotkeyKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!_recordingHotkey) return;
        e.Handled = true;

        if (e.Key == VirtualKey.Escape)
        {
            StopRecording();
            return;
        }

        // Modifier keys alone keep the recorder waiting for a main key.
        var gesture = HotkeyCapture.TryCapture(e.Key);
        if (gesture is null) return;

        ViewModel.Hotkey = gesture;
        StopRecording();
    }

    private void OnHotkeyLostFocus(object sender, RoutedEventArgs e) => StopRecording();

    private void StopRecording()
    {
        _recordingHotkey = false;
        HotkeyButton.Content = ViewModel.HotkeyDisplay;
    }
}
