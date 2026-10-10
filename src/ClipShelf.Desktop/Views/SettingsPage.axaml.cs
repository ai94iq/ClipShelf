using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClipShelf.Core.Export;
using ClipShelf.Core.Input;
using ClipShelf.Desktop.Platform.Windows;
using ClipShelf.Desktop.Shell;
using ClipShelf.Presentation.Features.Settings;

namespace ClipShelf.Desktop.Views;

// All eight settings groups, mirroring the WinUI page: appearance (with accent swatches),
// tray, behavior (with the hotkey recorder), history, export, categories, updates and exit.
// Password, rename and delete flows go through the shared dialogs; the page only owns the
// recorder state and the save-picker call.
public sealed partial class SettingsPage : UserControl
{
    private readonly Lazy<TrayController> _tray;
    private bool _recordingHotkey;
    private bool _syncingMaxItems;

    public SettingsPage(SettingsViewModel viewModel, Lazy<TrayController> tray)
    {
        ViewModel = viewModel;
        _tray = tray;
        InitializeComponent();
        DataContext = viewModel;
        SyncMaxItemsBox();
    }

    public SettingsViewModel ViewModel { get; }

    public Task LoadAsync() => Task.WhenAll(
        ViewModel.RefreshCategoriesAsync(), ViewModel.CheckForUpdatesOnceAsync());

    [SupportedOSPlatform("windows")]
    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows()) _tray.Value.Exit();
    }

    [SupportedOSPlatform("windows")]
    private void OnRestartClick(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows()) _tray.Value.Restart();
    }

    private async void OnCheckUpdatesClick(object? sender, RoutedEventArgs e) =>
        await ViewModel.CheckForUpdatesAsync();

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        if (ViewModel.HasLockPassword &&
            !await PasswordDialogs.VerifyAsync(owner, ViewModel.VerifyLockPassword))
            return;

        var isCsv = ViewModel.SelectedExportFormat.Value == ClipExportFormat.Csv;
        var extension = isCsv ? ".csv" : ".json";
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Tr.Get("Settings_Export"),
            SuggestedFileName = ViewModel.SuggestedExportFileName,
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(Tr.Get(isCsv ? "Export_Csv" : "Export_Json"))
                {
                    Patterns = [extension],
                }],
            ShowOverwritePrompt = true,
        });

        var path = file?.TryGetLocalPath();
        if (path is null) return;

        await ViewModel.ExportAsync(path);
    }

    private async void OnPasswordClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var password = ViewModel.HasLockPassword
            ? await PasswordDialogs.ChangeAsync(owner, ViewModel.VerifyLockPassword)
            : await PasswordDialogs.SetAsync(owner);
        if (password is not null) await ViewModel.SetLockPasswordAsync(password);
    }

    private async void OnRemovePasswordClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        if (await PasswordDialogs.ConfirmRemoveAsync(owner, ViewModel.VerifyLockPassword))
            await ViewModel.RemoveLockPasswordAsync();
    }

    private async void OnCategoryActionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: CategoryRow row, Tag: string action }) return;

        switch (action)
        {
            case "lock":
                // Locking needs the shared password first; there is nothing to ask if none is set.
                if (!ViewModel.HasLockPassword)
                {
                    if (TopLevel.GetTopLevel(this) is not Window setOwner) break;
                    var created = await PasswordDialogs.SetAsync(setOwner);
                    if (created is null) break;
                    await ViewModel.SetLockPasswordAsync(created);
                }

                await ViewModel.LockCategoryAsync(row.Id);
                break;
            case "locknow":
                await ViewModel.LockCategoryAsync(row.Id);
                break;
            case "unlock":
                if (TopLevel.GetTopLevel(this) is not Window verifyOwner) break;
                if (await PasswordDialogs.VerifyAsync(verifyOwner, ViewModel.VerifyLockPassword))
                    await ViewModel.UnlockCategoryAsync(row.Id);
                break;
            case "rename":
                await RenameCategoryAsync(row);
                break;
            case "delete":
                await DeleteCategoryAsync(row);
                break;
        }
    }

    private async Task RenameCategoryAsync(CategoryRow row)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var name = await RenameDialog.ShowAsync(
            owner, row.Name, candidate => IsCategoryNameTakenAsync(row.Id, candidate));
        if (name is not null) await ViewModel.RenameCategoryAsync(row.Id, name);
    }

    // The dialog loops while the name is taken; anything else closes it with the typed name.
    private Task<bool> IsCategoryNameTakenAsync(long id, string candidate)
    {
        var taken = ViewModel.CategoryRows.Any(row =>
            row.Id != id && string.Equals(row.Name, candidate.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(taken);
    }

    private async Task DeleteCategoryAsync(CategoryRow row)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var confirmed = await ConfirmDialog.ShowAsync(
            owner,
            Tr.Get("Category_DeleteTitle"),
            Tr.Format("Category_DeleteConfirm", row.Name),
            Tr.Get("Common_Delete"));
        if (confirmed) await ViewModel.DeleteCategoryAsync(row.Id);
    }

    private void OnHotkeyClick(object? sender, RoutedEventArgs e)
    {
        _recordingHotkey = true;
        HotkeyButton.Content = Tr.Get("Settings_HotkeyPrompt");
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

        // Modifier keys alone (and anything outside the table) keep the recorder waiting.
        var gesture = DesktopHotkey.TryCapture(e.Key, e.KeyModifiers);
        if (gesture is null) return;

        ViewModel.Hotkey = gesture;
        StopRecording();
    }

    private void OnHotkeyLostFocus(object? sender, RoutedEventArgs e) => StopRecording();

    private void StopRecording()
    {
        _recordingHotkey = false;
        HotkeyButton.Content = ViewModel.HotkeyDisplay;
    }

    private void OnHotkeyAlternativeClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is Option<HotkeyGesture> option)
            ViewModel.ApplyHotkeyAlternative(option.Value);
    }

    private void OnAccentChecked(object? sender, RoutedEventArgs e)
    {
        // IsCheckedChanged fires for unchecking too (when another swatch is picked); only a
        // fresh check selects.
        if ((sender as RadioButton)?.IsChecked != true) return;
        if ((sender as Control)?.DataContext is AccentOption option && ViewModel.SelectedAccent != option)
            ViewModel.SelectedAccent = option;
    }

    // NumericUpDown works in decimals while the setting is a double; sync through code instead
    // of a binding so no converter is needed. The guard skips the initial fill, which would
    // otherwise save the setting (and schedule a prune) on every page load.
    private void SyncMaxItemsBox()
    {
        _syncingMaxItems = true;
        try
        {
            MaxItemsBox.Value = (decimal)ViewModel.MaxItems;
        }
        finally
        {
            _syncingMaxItems = false;
        }
    }

    private void OnMaxItemsValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_syncingMaxItems) return;
        ViewModel.MaxItems = (double)(e.NewValue ?? 5);
    }
}
