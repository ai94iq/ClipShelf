using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;

namespace ClipShelf.App.Common;

public sealed partial class LoadStateOverlay : UserControl
{
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(LoadState), typeof(LoadStateOverlay), new PropertyMetadata(LoadState.Idle));

    public static readonly DependencyProperty ErrorMessageProperty = DependencyProperty.Register(
        nameof(ErrorMessage), typeof(string), typeof(LoadStateOverlay), new PropertyMetadata(null));

    public static readonly DependencyProperty EmptyTextProperty = DependencyProperty.Register(
        nameof(EmptyText), typeof(string), typeof(LoadStateOverlay), new PropertyMetadata(null));

    public static readonly DependencyProperty RetryCommandProperty = DependencyProperty.Register(
        nameof(RetryCommand), typeof(ICommand), typeof(LoadStateOverlay), new PropertyMetadata(null));

    public LoadStateOverlay() => InitializeComponent();

    public LoadState State
    {
        get => (LoadState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public string? EmptyText
    {
        get => (string?)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }
}
