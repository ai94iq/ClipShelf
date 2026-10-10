using System.Runtime.Versioning;
using Avalonia.Input;
using ClipShelf.Desktop.Views;

namespace ClipShelf.Desktop.Features;

// Keeps the flyout's keyboard and pointer commands together: arrows move the selection, Enter and
// Ctrl+1..9 pick, Esc steps back, a left click picks, a right click opens the category menu.
[SupportedOSPlatform("windows")]
internal sealed class FlyoutActions
{
    private readonly FlyoutWindow _window;
    private readonly ClipboardPanelViewModel _viewModel;
    private readonly IPasteBack _pasteBack;

    public FlyoutActions(FlyoutWindow window, ClipboardPanelViewModel viewModel, IPasteBack pasteBack)
    {
        _window = window;
        _viewModel = viewModel;
        _pasteBack = pasteBack;
        _viewModel.ItemActivated += OnItemActivated;
    }

    public void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                _viewModel.MoveSelection(1);
                e.Handled = true;
                return;
            case Key.Up:
                _viewModel.MoveSelection(-1);
                e.Handled = true;
                return;
            case Key.Enter:
                _viewModel.ActivateSelected();
                e.Handled = true;
                return;
            case Key.Escape:
                // Esc leaves select mode first, then cancels the confirmation, and only then closes.
                if (!_viewModel.DismissTransient()) _window.HideFlyout();
                e.Handled = true;
                return;
        }

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
        if (e.Key is < Key.D1 or > Key.D9) return;

        var index = (int)e.Key - (int)Key.D1;
        if (index < _viewModel.Items.Count) _viewModel.Activate(_viewModel.Items[index]);
        e.Handled = true;
    }

    public void ActivateItem(ClipItemViewModel item) => _viewModel.Activate(item);

    public Task OpenCategoryMenuAsync(ClipItemViewModel item) =>
        CategoryMenu.ShowAsync(_window, item, _viewModel);

    private void OnItemActivated(object? sender, EventArgs e)
    {
        _window.HideFlyout();
        _ = _pasteBack.IntoAsync(_window.PreviousWindowHandle);
    }
}
