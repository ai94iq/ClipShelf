using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClipShelf.Core.Export;
using ClipShelf.Core.Input;
using ClipShelf.Desktop.Platform;
using ClipShelf.Desktop.Shell;

namespace ClipShelf.Desktop.Features;

// The Settings section of the main window. Every change saves and applies at once; the view
// model owns that, the page only adds what needs the UI (pickers, the hotkey recorder).
public sealed partial class SettingsPage : UserControl
{
    private readonly Lazy<AppShellService> _shell;
    private bool _recordingHotkey;

    public SettingsPage(SettingsViewModel viewModel, Lazy<AppShellService> shell)
    {
        ViewModel = viewModel;
        _shell = shell;
        InitializeComponent();
        DataContext = viewModel;

        // Tunnel: the button must not turn Space or Enter into a click while recording.
        HotkeyButton.Click += (_, _) => StartRecording();
        HotkeyButton.AddHandler(KeyDownEvent, OnHotkeyKeyDown, RoutingStrategies.Tunnel);
        HotkeyButton.LostFocus += (_, _) => StopRecording();
    }

    public SettingsViewModel ViewModel { get; }

    private void OnRestartClick(object? sender, RoutedEventArgs e) => _shell.Value.Restart();

    private void OnAccentCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true, DataContext: AccentOption option } && ViewModel.SelectedAccent != option)
            ViewModel.SelectedAccent = option;
    }

    private void StartRecording()
    {
        _recordingHotkey = true;
        ShowPrompt(true);
        HotkeyButton.Focus();
    }

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_recordingHotkey) return;
        e.Handled = true;

        if (e.Key == Key.Escape)
        {
            StopRecording();
            return;
        }

        // Modifier keys alone keep the recorder waiting for a main key.
        if (HotkeyCapture.TryCapture(e.Key, e.KeyModifiers) is not { } gesture) return;

        ViewModel.Hotkey = gesture;
        StopRecording();
    }

    private void StopRecording()
    {
        _recordingHotkey = false;
        ShowPrompt(false);
    }

    private void ShowPrompt(bool recording)
    {
        HotkeyPrompt.IsVisible = recording;
        HotkeyText.IsVisible = !recording;
    }

    private void OnHotkeyAlternativeClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is Option<HotkeyGesture> option)
            ViewModel.ApplyHotkeyAlternative(option.Value);
    }

    private void OnExitClick(object? sender, RoutedEventArgs e) => _shell.Value.Exit();

    private async void OnCheckUpdatesClick(object? sender, RoutedEventArgs e) => await ViewModel.CheckForUpdatesAsync();

    // A password exists whenever these buttons show, so locking needs no prompt.
    private async void OnLockClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is CategoryRow row) await ViewModel.LockCategoryAsync(row.Id);
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { CanSave: true } storage) return;

        var csv = ViewModel.SelectedExportFormat.Value == ClipExportFormat.Csv;
        var extension = csv ? "csv" : "json";
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = ViewModel.SuggestedExportFileName,
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(Tr.Get(csv ? "Export_Csv" : "Export_Json")) { Patterns = [$"*.{extension}"] }],
            SuggestedStartLocation = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents),
        });
        if (file?.TryGetLocalPath() is not { } path) return;

        await ViewModel.ExportAsync(path);
    }

    public Task LoadAsync() => Task.WhenAll(ViewModel.RefreshCategoriesAsync(), ViewModel.CheckForUpdatesOnceAsync());
}
