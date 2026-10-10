using Avalonia;
using Avalonia.Controls;

namespace ClipShelf.Desktop.Controls;

// One settings row in a card: a title (with an optional description) on the left, its control on
// the right. The child element written inside the tag becomes Content. The default style lives in
// Themes/Controls.axaml; the description line only shows when Description is non-empty.
public sealed class SettingsCard : ContentControl
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<SettingsCard, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingsCard, string?>(nameof(Description));

    static SettingsCard()
    {
        DescriptionProperty.Changed.AddClassHandler<SettingsCard>((c, _) => c.UpdateHasDescription());
    }

    public SettingsCard() => UpdateHasDescription();

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    private void UpdateHasDescription() =>
        PseudoClasses.Set(":has-description", !string.IsNullOrWhiteSpace(Description));
}
