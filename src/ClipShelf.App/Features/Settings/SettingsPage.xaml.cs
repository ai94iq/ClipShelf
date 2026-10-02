using ClipShelf.App.Platform;
using ClipShelf.App.Shell;
using ClipShelf.Core.Export;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

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

    public Task LoadAsync() => Task.WhenAll(
        ViewModel.RefreshCategoriesAsync(), ViewModel.CheckForUpdatesOnceAsync());

    private void OnExit(object sender, RoutedEventArgs e) => _shell.Value.Exit();

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e) =>
        await ViewModel.CheckForUpdatesAsync();

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.HasLockPassword &&
            !await PasswordPrompt.VerifyAsync(XamlRoot, ViewModel.VerifyLockPassword))
            return;

        var format = ViewModel.SelectedExportFormat.Value;
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = ViewModel.SuggestedExportFileName,
        };
        picker.FileTypeChoices.Add(
            Tr.Get(format == ClipExportFormat.Csv ? "Export_Csv" : "Export_Json"),
            [format == ClipExportFormat.Csv ? ".csv" : ".json"]);
        InitializeWithWindow.Initialize(picker, _shell.Value.MainWindowHandle);

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        await ViewModel.ExportAsync(file.Path);
    }

    private async void OnPasswordClick(object sender, RoutedEventArgs e)
    {
        var password = ViewModel.HasLockPassword
            ? await PasswordPrompt.ChangeAsync(XamlRoot, ViewModel.VerifyLockPassword)
            : await PasswordPrompt.SetAsync(XamlRoot);
        if (password is not null) await ViewModel.SetLockPasswordAsync(password);
    }

    private async void OnRemovePasswordClick(object sender, RoutedEventArgs e)
    {
        if (await PasswordPrompt.ConfirmRemoveAsync(XamlRoot, ViewModel.VerifyLockPassword))
            await ViewModel.RemoveLockPasswordAsync();
    }

    private async void OnCategoryActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: CategoryRow row } element) return;

        switch (element.Tag as string)
        {
            case "lock":
                // Locking needs the shared password first; there is nothing to ask if none is set.
                if (!ViewModel.HasLockPassword)
                {
                    var created = await PasswordPrompt.SetAsync(XamlRoot);
                    if (created is null) break;
                    await ViewModel.SetLockPasswordAsync(created);
                }

                await ViewModel.LockCategoryAsync(row.Id);
                break;
            case "locknow":
                await ViewModel.LockCategoryAsync(row.Id);
                break;
            case "unlock":
                if (await PasswordPrompt.VerifyAsync(XamlRoot, ViewModel.VerifyLockPassword))
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
        var box = new TextBox { Text = row.Name };
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(box);
        content.Children.Add(error);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Tr.Get("Category_RenameTitle"),
            Content = content,
            PrimaryButtonText = Tr.Get("Common_Save"),
            CloseButtonText = Tr.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(row.Name),
        };
        box.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(box.Text);
        dialog.PrimaryButtonClick += async (_, args) =>
        {
            // Keep the dialog open while the name is checked, and on a taken name.
            var deferral = args.GetDeferral();
            try
            {
                if (await ViewModel.RenameCategoryAsync(row.Id, box.Text)) return;

                error.Text = Tr.Get("Category_Exists");
                error.Visibility = Visibility.Visible;
                args.Cancel = true;
            }
            finally
            {
                deferral.Complete();
            }
        };

        await dialog.ShowAsync();
    }

    private async Task DeleteCategoryAsync(CategoryRow row)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Tr.Get("Category_DeleteTitle"),
            Content = Tr.Format("Category_DeleteConfirm", row.Name),
            PrimaryButtonText = Tr.Get("Common_Delete"),
            CloseButtonText = Tr.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Close,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.DeleteCategoryAsync(row.Id);
    }

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
