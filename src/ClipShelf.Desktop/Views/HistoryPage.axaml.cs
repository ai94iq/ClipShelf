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

    // Parameterless constructor for XAML designer/loader
    public HistoryPage() => InitializeComponent();

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

    public ClipboardPanelViewModel? ViewModel { get; }

    // True while the shell window shows this page; the shared view model's menu request must only
    // be handled by the window the user actually clicked in.
    public bool IsActive { get; set; }

    public Task LoadAsync() =>
        Task.WhenAll(ViewModel!.RefreshAsync(), ViewModel!.RefreshCategoryFilterAsync());

    // Click-to-copy runs through the row's ActivateCommand; the accent wash belongs to the
    // keyboard selection and Select mode, not to the last clicked row.
    private void OnRowClick(object? sender, RoutedEventArgs e) => ViewModel!.SelectedItem = null;

    // Up and Down move the view-model selection (the accent wash follows); Enter picks the
    // selection; Esc leaves select mode or cancels the clear confirmation, the page stays open.
    // The filter combo keeps its own arrows.
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Source is ComboBox) return;

        if (e.Key is Key.Up or Key.Down)
        {
            ViewModel!.MoveSelection(e.Key == Key.Down ? 1 : -1);
            FocusSelectedRow();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            ViewModel!.ActivateSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = ViewModel!.DismissTransient();
        }
    }

    // Moves keyboard focus to the selected row so screen readers and Enter stay on the same clip.
    private void FocusSelectedRow()
    {
        if (ViewModel!.SelectedItem is not { } selected) return;
        this.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .FirstOrDefault(row => ReferenceEquals(row.DataContext, selected))
            ?.Focus();
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
        if (e.Source is Button) return;
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;
        if ((e.Source as Control)?.DataContext is not ClipItemViewModel item) return;

        e.Handled = true;
        OnCategoryMenuRequested(item);
    }

    // Left click picks the row, mirroring the WinUI ItemClick path. Action buttons handle
    // their own release, so a release landing on one never reaches here.
    private void OnGroupsPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left) return;
        if (e.Source is Button) return;
        if ((e.Source as Control)?.DataContext is ClipItemViewModel item) ViewModel!.Activate(item);
    }

    private void OnCategoryMenuRequested(ClipItemViewModel item)
    {
        // The view model is shared with the flyout; only this page's own clicks open its menu.
        if (!IsActive) return;

        _ = CategoryMenu.ShowAsync(this, item, ViewModel!);
    }

    // Picking a locked category asks for its password instead of showing an empty list.
    private async void OnCategoryFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = ViewModel!.SelectedCategoryFilter;
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
        if (ViewModel!.SelectedCategoryFilter?.Value is not long id || !ViewModel.IsCategoryFilterLocked)
            return false;

        if (TopLevel.GetTopLevel(this) is not Window owner) return false;
        if (!await PasswordDialogs.VerifyAsync(owner, ViewModel!.VerifyLockPassword)) return false;

        await ViewModel.UnlockCategoryAsync(id);
        _lastFilterValue = id;
        return true;
    }
}
