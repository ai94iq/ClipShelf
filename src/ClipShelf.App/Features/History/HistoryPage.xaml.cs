using ClipShelf.App.Features.ClipboardPanel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace ClipShelf.App.Features.History;

// The History page of the app shell: the full list, grouped into Pinned and Recent.
public sealed partial class HistoryPage : UserControl
{
    private readonly CollectionViewSource _grouped = new() { IsSourceGrouped = true };

    public HistoryPage(ClipboardPanelViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        _grouped.Source = viewModel.Groups;
        ClipList.ItemsSource = _grouped.View;
        DestructiveHover.TintOnHover(ClearAllButton);
        DestructiveHover.TintOnHover(ConfirmClearButton);
    }

    public ClipboardPanelViewModel ViewModel { get; }

    public Task LoadAsync() => ViewModel.RefreshAsync();

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ClipItemViewModel item) ViewModel.Activate(item);
    }
}
