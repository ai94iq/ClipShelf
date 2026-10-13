using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace ClipShelf.Desktop.Controls;

// Main's ContentDialog, drawn inside the window on its overlay layer. A separate modal window
// would take the focus, and the flyout hides as soon as it loses it. Enter accepts and Esc
// cancels; window key handlers stand aside while a dialog is open (IsOpenIn).
public sealed partial class InlineDialog : UserControl
{
    private readonly TaskCompletionSource<bool> _result = new();
    private OverlayLayer? _layer;

    public InlineDialog(string title, string primaryText, params Control[] content)
    {
        InitializeComponent();
        TitleText.Text = title;
        PrimaryButton.Content = primaryText;
        foreach (var control in content) ContentHost.Children.Add(control);

        PrimaryButton.Click += (_, _) => UiTask.Run(AcceptAsync, "A dialog action");
        CancelButton.Click += (_, _) => Close(false);
        // Tunnel: a text box would take Enter before it bubbled up here.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    // Runs on the primary button or Enter; false keeps the dialog open (a wrong password).
    public Func<Task<bool>>? Accept { get; set; }

    public bool IsPrimaryEnabled
    {
        get => PrimaryButton.IsEnabled;
        set => PrimaryButton.IsEnabled = value;
    }

    public static InlineDialog? OpenIn(Visual visual) =>
        OverlayLayer.GetOverlayLayer(visual)?.Children.OfType<InlineDialog>().LastOrDefault();

    public static bool IsOpenIn(Visual visual) => OpenIn(visual) is not null;

    public void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    // True when accepted, false when cancelled.
    public Task<bool> ShowAsync(Visual anchor)
    {
        _layer = OverlayLayer.GetOverlayLayer(anchor)
            ?? throw new InvalidOperationException("The anchor is not in a window.");
        Width = _layer.Bounds.Width;
        Height = _layer.Bounds.Height;
        _layer.PropertyChanged += OnLayerResized;
        _layer.Children.Add(this);

        // The first input takes the focus once the card is laid out.
        Dispatcher.UIThread.Post(() =>
        {
            var first = ContentHost.Children.OfType<InputElement>().FirstOrDefault(c => c.Focusable) ?? PrimaryButton;
            first.Focus();
        });
        return _result.Task;
    }

    private void OnLayerResized(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != BoundsProperty || _layer is null) return;
        Width = _layer.Bounds.Width;
        Height = _layer.Bounds.Height;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(false);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (IsPrimaryEnabled) UiTask.Run(AcceptAsync, "A dialog action");
            e.Handled = true;
        }
    }

    private async Task AcceptAsync()
    {
        if (Accept is not null && !await Accept()) return;
        Close(true);
    }

    private void Close(bool accepted)
    {
        if (_layer is not null)
        {
            _layer.PropertyChanged -= OnLayerResized;
            _layer.Children.Remove(this);
        }

        _result.TrySetResult(accepted);
    }
}
