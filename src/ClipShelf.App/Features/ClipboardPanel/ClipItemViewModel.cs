using ClipShelf.Core.Models;

namespace ClipShelf.App.Features.ClipboardPanel;

// One row in the history list. Its commands delegate to the panel so the window stays dumb.
public sealed partial class ClipItemViewModel : ObservableObject
{
    private readonly ClipboardPanelViewModel _panel;

    public ClipItemViewModel(ClipListItem model, IDateFormatter dates, ClipboardPanelViewModel panel)
    {
        _panel = panel;
        Id = model.Id;
        Text = model.Text;
        AppName = model.AppName;
        IsPinned = model.IsPinned;
        TimeText = dates.FormatSince(model.CreatedAtUtc);
    }

    public long Id { get; }

    public string Text { get; }

    public string? AppName { get; }

    public string TimeText { get; }

    public string Preview => ClipText.Preview(Text);

    public string SourceText => string.IsNullOrEmpty(AppName) ? TimeText : $"{AppName} · {TimeText}";

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [RelayCommand]
    private void Activate() => _panel.Activate(this);

    [RelayCommand]
    private Task TogglePinAsync() => _panel.TogglePinAsync(this);

    [RelayCommand]
    private Task DeleteAsync() => _panel.DeleteAsync(this);
}
