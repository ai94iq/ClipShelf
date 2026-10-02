using ClipShelf.App.Platform;
using ClipShelf.App.Shell;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

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
        _viewModel.CategoryMenuRequested += OnCategoryMenuRequested;
    }

    public void OnItemClick(ItemClickEventArgs e)
    {
        if (e.ClickedItem is ClipItemViewModel item) _viewModel.Activate(item);
    }

    public void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (HandleNavigation(e)) return;

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

    // Arrow keys move the selection, Enter pastes it, and Ctrl+1..9 paste one of the first nine.
    private bool HandleNavigation(KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Down:
                _viewModel.MoveSelection(1);
                e.Handled = true;
                return true;
            case VirtualKey.Up:
                _viewModel.MoveSelection(-1);
                e.Handled = true;
                return true;
            case VirtualKey.Enter:
                _viewModel.ActivateSelected();
                e.Handled = true;
                return true;
        }

        if (!IsControlDown() || e.Key is < VirtualKey.Number1 or > VirtualKey.Number9) return false;

        var index = (int)e.Key - (int)VirtualKey.Number1;
        if (index < _viewModel.Items.Count) _viewModel.Activate(_viewModel.Items[index]);
        e.Handled = true;
        return true;
    }

    private static bool IsControlDown() =>
        InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(CoreVirtualKeyStates.Down);

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

    private void OnCategoryMenuRequested(ClipItemViewModel item) =>
        _ = CategoryMenu.ShowAsync(_window.RowAnchor(item), item, _viewModel);
}
