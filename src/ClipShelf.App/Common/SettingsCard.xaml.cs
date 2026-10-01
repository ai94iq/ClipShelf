using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace ClipShelf.App.Common;

// One settings row in a card: a title (with an optional description) on the left, its control on
// the right. The child element written inside the tag becomes CardContent.
[ContentProperty(Name = nameof(CardContent))]
public sealed partial class SettingsCard : UserControl
{
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(SettingsCard), new PropertyMetadata(string.Empty, OnHeaderChanged));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(SettingsCard), new PropertyMetadata(null, OnDescriptionChanged));

    public static readonly DependencyProperty CardContentProperty = DependencyProperty.Register(
        nameof(CardContent), typeof(object), typeof(SettingsCard), new PropertyMetadata(null, OnCardContentChanged));

    public SettingsCard() => InitializeComponent();

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? CardContent
    {
        get => GetValue(CardContentProperty);
        set => SetValue(CardContentProperty, value);
    }

    private static void OnHeaderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((SettingsCard)sender).HeaderText.Text = (string)args.NewValue;

    private static void OnDescriptionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var card = (SettingsCard)sender;
        card.DescriptionText.Text = (string?)args.NewValue ?? string.Empty;
        card.DescriptionText.Visibility = string.IsNullOrWhiteSpace(args.NewValue as string)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private static void OnCardContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((SettingsCard)sender).CardHost.Content = args.NewValue;
}
