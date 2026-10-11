using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ClipShelf.Presentation.Common;
using System.Windows.Input;

namespace ClipShelf.Desktop.Controls;

// Loading/empty/error overlay for pages. Bind State, ErrorMessage, EmptyTitle, EmptyHint, RetryCommand.
public partial class LoadStateOverlay : UserControl
{
    public LoadStateOverlay() => InitializeComponent();

    public static readonly StyledProperty<LoadState> StateProperty =
        AvaloniaProperty.Register<LoadStateOverlay, LoadState>(nameof(State), LoadState.Idle);

    public LoadState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<LoadStateOverlay, string?>(nameof(ErrorMessage));

    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public static readonly StyledProperty<string?> EmptyTitleProperty =
        AvaloniaProperty.Register<LoadStateOverlay, string?>(nameof(EmptyTitle));

    public string? EmptyTitle
    {
        get => GetValue(EmptyTitleProperty);
        set => SetValue(EmptyTitleProperty, value);
    }

    public static readonly StyledProperty<string?> EmptyHintProperty =
        AvaloniaProperty.Register<LoadStateOverlay, string?>(nameof(EmptyHint));

    public string? EmptyHint
    {
        get => GetValue(EmptyHintProperty);
        set => SetValue(EmptyHintProperty, value);
    }

    public static readonly StyledProperty<ICommand?> RetryCommandProperty =
        AvaloniaProperty.Register<LoadStateOverlay, ICommand?>(nameof(RetryCommand));

    public ICommand? RetryCommand
    {
        get => GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }
}