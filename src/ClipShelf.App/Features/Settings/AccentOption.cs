namespace ClipShelf.App.Features.Settings;

// One accent swatch: a preset hex, or null for "Windows accent". IsSelected mirrors the current
// choice so the radio swatches can bind to it; the view model keeps it in sync.
public sealed partial class AccentOption : ObservableObject
{
    public AccentOption(string? value, string label)
    {
        Value = value;
        Label = label;
    }

    public string? Value { get; }

    public string Label { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
