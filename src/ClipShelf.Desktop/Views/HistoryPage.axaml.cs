using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClipShelf.Desktop.Features;

namespace ClipShelf.Desktop.Views;

// The full history list, grouped into Pinned and Recent. It shares its view model with the
// flyout; IsActive marks the window the user actually clicked in so menu requests land in
// exactly one place.
public sealed partial class HistoryPage : UserControl
{
    private long? _lastFilterValue;

    public HistoryPage(ClipboardPanelViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CategoryMenuRequested += OnCategoryMenuRequested;

        // Tunneling, so arrows and Esc work while the search box holds focus.
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        GroupsList.AddHandler(Button.ClickEvent, OnRowClick);
    }

    public ClipboardPanelViewModel ViewModel { get; }

    // True while the shell window shows this page; the shared view model's menu request must only
    // be handled by the window the user actually clicked in.
    public bool IsActive { get; set; }

    public Task LoadAsync() =>
        Task.WhenAll(ViewModel.RefreshAsync(), ViewModel.RefreshCategoryFilterAsync());

    // Click-to-copy runs through the row's ActivateCommand; the accent wash belongs to the
    // keyboard selection and Select mode, not to the last clicked row.
    private void OnRowClick(object? sender, RoutedEventArgs e) => ViewModel.SelectedItem = null;

    // Up and Down move the focus through the rows; Enter activates natively through the focused
    // button; Esc leaves select mode or cancels the clear confirmation, the page stays open.
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down)
        {
            MoveRowFocus(e.Key == Key.Down ? 1 : -1);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = ViewModel.DismissTransient();
        }
    }

    // Moves the keyboard focus to the next or previous row button, in visual order.
    internal void MoveRowFocus(int delta)
    {
        var rows = this.GetVisualDescendants()
            .OfType<Button>()
            .Where(button => button.Classes.Contains("clip-row"))
            .ToList();
        if (rows.Count == 0) return;

        var focused = rows.FindIndex(button => button.IsFocused);
        var next = focused < 0
            ? (delta > 0 ? 0 : rows.Count - 1)
            : Math.Clamp(focused + delta, 0, rows.Count - 1);
        rows[next].Focus();
    }

    // Loads the next page once the scroll is within 200 px of the bottom.
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer viewer) return;
        if (viewer.Offset.Y + viewer.Viewport.Height < viewer.Extent.Height - 200) return;
        if (ViewModel is not IIncrementalSource { HasMore: true } source) return;

        if (source.LoadMoreCommand.CanExecute(null)) source.LoadMoreCommand.Execute(null);
    }

    private void OnGroupsPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;
        if ((e.Source as Control)?.DataContext is not ClipItemViewModel item) return;

        e.Handled = true;
        OnCategoryMenuRequested(item);
    }

    private void OnCategoryMenuRequested(ClipItemViewModel item)
    {
        // The view model is shared with the flyout; only this page's own clicks open its menu.
        if (!IsActive) return;

        _ = CategoryMenu.ShowAsync(this, item, ViewModel);
    }

    // Picking a locked category asks for its password instead of showing an empty list.
    private async void OnCategoryFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = ViewModel.SelectedCategoryFilter;
        if (selected is null || selected.Value == _lastFilterValue) return;

        if (!ViewModel.IsCategoryFilterLocked)
        {
            _lastFilterValue = selected.Value;
            return;
        }

        if (await UnlockSelectedFilterAsync()) return;

        ViewModel.SelectedCategoryFilter =
            ViewModel.CategoryFilterOptions.FirstOrDefault(option => option.Value == _lastFilterValue)
            ?? ViewModel.CategoryFilterOptions[0];
    }

    // Picking the category that is already selected raises no selection change, so ask again
    // whenever the list closes on a locked category.
    private async void OnCategoryFilterDropDownClosed(object? sender, EventArgs e) =>
        await UnlockSelectedFilterAsync();

    private async Task<bool> UnlockSelectedFilterAsync()
    {
        if (ViewModel.SelectedCategoryFilter?.Value is not long id || !ViewModel.IsCategoryFilterLocked)
            return false;

        if (TopLevel.GetTopLevel(this) is not Window owner) return false;
        if (!await PasswordDialogs.VerifyAsync(owner, ViewModel.VerifyLockPassword)) return false;

        await ViewModel.UnlockCategoryAsync(id);
        _lastFilterValue = id;
        return true;
    }
}
