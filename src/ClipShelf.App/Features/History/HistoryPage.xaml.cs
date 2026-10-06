using ClipShelf.App.Features.ClipboardPanel;
using ClipShelf.App.Features.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace ClipShelf.App.Features.History;

// The History page of the app shell: the full list, grouped into Pinned and Recent.
public sealed partial class HistoryPage : UserControl
{
    private readonly CollectionViewSource _grouped = new() { IsSourceGrouped = true };
    private long? _lastFilterValue;

    public HistoryPage(ClipboardPanelViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        _grouped.Source = viewModel.Groups;
        ClipList.ItemsSource = _grouped.View;
        DestructiveHover.TintOnHover(ClearAllButton);
        DestructiveHover.TintOnHover(ConfirmClearButton);
        viewModel.CategoryMenuRequested += OnCategoryMenuRequested;
    }

    public ClipboardPanelViewModel ViewModel { get; }

    public Task LoadAsync() => Task.WhenAll(ViewModel.RefreshAsync(), ViewModel.RefreshCategoryFilterAsync());

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ClipItemViewModel item) ViewModel.Activate(item);
    }

    // Loads the next page once the list realizes its last row (the user scrolled to the end).
    private void OnClipListContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase != 0 || args.InRecycleQueue) return;
        if (args.Item is not ClipItemViewModel item) return;
        if (!ReferenceEquals(item, ViewModel.Items.LastOrDefault())) return;

        if (ViewModel.LoadMoreCommand.CanExecute(null)) ViewModel.LoadMoreCommand.Execute(null);
    }

    private void OnCategoryMenuRequested(ClipItemViewModel item) =>
        _ = CategoryMenu.ShowAsync(ClipList.ContainerFromItem(item) as FrameworkElement ?? ClipList, item, ViewModel);

    // Picking a locked category asks for its password instead of showing an empty list.
    private async void OnCategoryFilterChanged(object sender, SelectionChangedEventArgs e)
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
    private async void OnCategoryFilterDropDownClosed(object sender, object e) =>
        await UnlockSelectedFilterAsync();

    private async Task<bool> UnlockSelectedFilterAsync()
    {
        if (ViewModel.SelectedCategoryFilter?.Value is not long id || !ViewModel.IsCategoryFilterLocked)
            return false;

        if (!await PasswordPrompt.VerifyAsync(XamlRoot, ViewModel.VerifyLockPassword)) return false;

        await ViewModel.UnlockCategoryAsync(id);
        _lastFilterValue = id;
        return true;
    }
}
