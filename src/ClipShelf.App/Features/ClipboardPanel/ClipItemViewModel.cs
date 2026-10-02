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
    public partial string? CategoryName { get; set; }

    [ObservableProperty]
    public partial string TimeText { get; set; } = string.Empty;

    public long? CategoryId { get; private set; }

    public string Preview => ClipText.Preview(Text);

    public string SourceText
    {
        get
        {
            var category = CategoryName;
            var app = AppName;
            var parts = new List<string>(3);
            if (!string.IsNullOrEmpty(category)) parts.Add(category);
            if (!string.IsNullOrEmpty(app)) parts.Add(app);
            parts.Add(TimeText);
            return string.Join(" · ", parts);
        }
    }

    public void UpdateFrom(ClipListItem model, IDateFormatter dates)
    {
        Text = model.Text;
        AppName = AppNameText.Display(model.AppName);
        CategoryId = model.CategoryId;
        CategoryName = model.CategoryName;
        IsPinned = model.IsPinned;
        HasImage = model.HasImage;
        ThumbnailBytes = model.ThumbnailBytes;
        TimeText = dates.FormatSince(model.CreatedAtUtc);
    }

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool HasImage { get; set; }

    // The bytes the row template turns into the thumbnail.
    [ObservableProperty]
    public partial byte[]? ThumbnailBytes { get; set; }

    // Image clips show their thumbnail instead of the text preview.
    public bool ShowPreview => !HasImage;

    partial void OnHasImageChanged(bool value) => OnPropertyChanged(nameof(ShowPreview));

    // The checkbox only exists while the panel is in select mode.
    public bool ShowSelection => _panel.IsSelecting;

    public void NotifySelectionModeChanged() => OnPropertyChanged(nameof(ShowSelection));

    partial void OnIsSelectedChanged(bool value) => _panel.NotifySelectionChanged();

    [RelayCommand]
    private void Activate() => _panel.Activate(this);

    [RelayCommand]
    private Task TogglePinAsync() => _panel.TogglePinAsync(this);

    [RelayCommand]
    private Task DeleteAsync() => _panel.DeleteAsync(this);

    [RelayCommand]
    private void Category() => _panel.RequestCategoryMenu(this);

    partial void OnTextChanged(string value) => OnPropertyChanged(nameof(Preview));

    partial void OnAppNameChanged(string? value) => OnPropertyChanged(nameof(SourceText));

    partial void OnCategoryNameChanged(string? value) => OnPropertyChanged(nameof(SourceText));

    partial void OnTimeTextChanged(string value) => OnPropertyChanged(nameof(SourceText));
}
