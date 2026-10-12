using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClipShelf.Desktop.Controls;

namespace ClipShelf.Desktop.Features;

// The History page of the main window: the full list, grouped into Pinned and Recent.
public sealed partial class HistoryPage : UserControl
{
    // Within about one row of the end, the next page loads.
    private const double LoadMoreThreshold = 48;

    private long? _lastFilterValue;

    public HistoryPage(ClipboardPanelViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        ClipList.ItemsSource = new GroupedRows(viewModel.Groups);

        // Group headers are labels, not rows.
        ClipList.ContainerPrepared += (_, e) =>
        {
            var isHeader = e.Container.DataContext is ClipGroup;
            e.Container.IsEnabled = !isHeader;
            e.Container.Focusable = !isHeader;
        };
        ClipList.AddHandler(PointerReleasedEvent, OnListClicked, RoutingStrategies.Bubble, handledEventsToo: true);
        ClipList.AddHandler(ScrollViewer.ScrollChangedEvent, OnScrolled);
        CategoryFilter.SelectionChanged += (_, _) => OnCategoryFilterChanged();
    }

    public ClipboardPanelViewModel ViewModel { get; }

    public Task LoadAsync() => Task.WhenAll(ViewModel.RefreshAsync(), ViewModel.RefreshCategoryFilterAsync());

    // Keys are taken at the window, so Esc works even when nothing on the page has focus yet.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TopLevel.GetTopLevel(this)?.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        TopLevel.GetTopLevel(this)?.RemoveHandler(KeyDownEvent, OnKeyDown);
        base.OnDetachedFromVisualTree(e);
    }

    public static bool IsNearEnd(double offset, double viewport, double extent) =>
        offset + viewport >= extent - LoadMoreThreshold;

    // Enter copies the keyboard row; Esc leaves select mode or cancels the confirm, never more.
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ClipList.SelectedItem is ClipItemViewModel item && ClipList.IsKeyboardFocusWithin)
        {
            ViewModel.Activate(item);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = ViewModel.DismissTransient();
        }
    }

    // Click-to-copy: the accent tint belongs to the keyboard row and select mode, so a click
    // does not leave the row selected. Clicks on the row buttons stay with the buttons.
    private void OnListClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left || e.Source is not Visual source) return;
        if (source.FindAncestorOfType<Button>(includeSelf: true) is not null) return;
        if (source.FindAncestorOfType<ListBoxItem>(includeSelf: true)?.DataContext is not ClipItemViewModel item) return;

        ViewModel.Activate(item);
        if (!ViewModel.IsSelecting) ClipList.SelectedItem = null;
    }

    private void OnScrolled(object? sender, ScrollChangedEventArgs e)
    {
        if (e.Source is not ScrollViewer scroller) return;
        if (!IsNearEnd(scroller.Offset.Y, scroller.Viewport.Height, scroller.Extent.Height)) return;
        if (ViewModel is IIncrementalSource { HasMore: true } source && source.LoadMoreCommand.CanExecute(null))
            source.LoadMoreCommand.Execute(null);
    }

    // ponytail: a locked category needs the password dialog (batch 7); until then the filter
    // falls back to the previous choice instead of showing an empty list.
    private void OnCategoryFilterChanged()
    {
        var selected = ViewModel.SelectedCategoryFilter;
        if (selected is null || selected.Value == _lastFilterValue) return;

        if (!ViewModel.IsCategoryFilterLocked)
        {
            _lastFilterValue = selected.Value;
            return;
        }

        ViewModel.SelectedCategoryFilter =
            ViewModel.CategoryFilterOptions.FirstOrDefault(option => option.Value == _lastFilterValue)
            ?? ViewModel.CategoryFilterOptions[0];
    }
}
