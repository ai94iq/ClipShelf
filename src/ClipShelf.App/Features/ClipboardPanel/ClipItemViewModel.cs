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
        UpdateFrom(model, dates);
    }

    public long Id { get; }

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? AppName { get; set; }

    [ObservableProperty]
    public partial string TimeText { get; set; } = string.Empty;

    public string Preview => ClipText.Preview(Text);

    public string SourceText => string.IsNullOrEmpty(AppName) ? TimeText : $"{AppName} · {TimeText}";

    public void UpdateFrom(ClipListItem model, IDateFormatter dates)
    {
        Text = model.Text;
        AppName = AppNameText.Display(model.AppName);
        IsPinned = model.IsPinned;
        TimeText = dates.FormatSince(model.CreatedAtUtc);
    }

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [RelayCommand]
    private void Activate() => _panel.Activate(this);

    [RelayCommand]
    private Task TogglePinAsync() => _panel.TogglePinAsync(this);

    [RelayCommand]
    private Task DeleteAsync() => _panel.DeleteAsync(this);

    partial void OnTextChanged(string value) => OnPropertyChanged(nameof(Preview));

    partial void OnAppNameChanged(string? value) => OnPropertyChanged(nameof(SourceText));

    partial void OnTimeTextChanged(string value) => OnPropertyChanged(nameof(SourceText));
}
