using ClipShelf.App.Platform;
using ClipShelf.App.Shell;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace ClipShelf.App.Features.ClipboardPanel;

// Keeps panel commands that cross from the ViewModel into its window and app shell together.
internal sealed class ClipboardPanelActions
{
    private readonly ClipboardPanelWindow _window;
    private readonly ClipboardPanelViewModel _viewModel;
    private readonly SettingsService _settings;
    private readonly Lazy<AppShellService> _shell;

    public ClipboardPanelActions(
        ClipboardPanelWindow window,
        ClipboardPanelViewModel viewModel,
        SettingsService settings,
        Lazy<AppShellService> shell)
    {
        _window = window;
        _viewModel = viewModel;
        _settings = settings;
        _shell = shell;

        _viewModel.ItemActivated += OnItemActivated;
        _viewModel.OpenHistoryRequested += OnOpenHistoryRequested;
        _viewModel.SelectionCopied += (_, _) => _window.Hide();
    }

    public void OnItemClick(ItemClickEventArgs e)
    {
        if (e.ClickedItem is ClipItemViewModel item) _viewModel.Activate(item);
    }

    public void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape) return;

        // Esc leaves select mode first, then cancels the confirmation, and only then closes the flyout.
        if (_viewModel.IsSelecting)
            _viewModel.ToggleSelectionCommand.Execute(null);
        else if (_viewModel.IsConfirmingClear)
            _viewModel.CancelClearCommand.Execute(null);
        else
            _window.Hide();

        e.Handled = true;
    }

    private void OnItemActivated(object? sender, EventArgs e)
    {
        _window.Hide();
        if (_settings.Current.PasteOnSelect) _ = KeyboardPaste.IntoAsync(_window.PreviousWindowHandle);
    }

    private void OnOpenHistoryRequested(object? sender, EventArgs e)
    {
        _window.Hide();
        _shell.Value.ShowHistory();
    }
}
