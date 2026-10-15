using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace Copyloft.Desktop.Tests;

// Avalonia's FluentTheme carries Fluent v1 control fills (buttons 20% black, inputs 40% white with
// a 60% black border), heavier than the WinUI 3 controls main shows. The exceptions file maps the
// rest states onto WinUI 3's ControlFillColorDefault and ControlStrokeColorDefault.
public sealed class ControlFillTests
{
    [Theory]
    [InlineData("Light", "ButtonBackground", "#B3FFFFFF")]
    [InlineData("Light", "ButtonBorderBrush", "#0F000000")]
    [InlineData("Light", "ComboBoxBackground", "#B3FFFFFF")]
    [InlineData("Light", "ComboBoxBorderBrush", "#29000000")]
    [InlineData("Light", "TextControlBackground", "#B3FFFFFF")]
    [InlineData("Light", "TextControlBorderBrush", "#29000000")]
    [InlineData("Dark", "ButtonBackground", "#0FFFFFFF")]
    [InlineData("Dark", "ButtonBorderBrush", "#12FFFFFF")]
    [InlineData("Dark", "ComboBoxBackground", "#0FFFFFFF")]
    [InlineData("Dark", "TextControlBackground", "#0FFFFFFF")]
    [InlineData("Dark", "TextControlBorderBrush", "#28FFFFFF")]
    public Task Controls_rest_on_winui_3_fills(string variant, string key, string expected) => Ui.Run(() =>
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        Assert.True(Application.Current!.TryGetResource(key, theme, out var value), key);
        Assert.Equal(Color.Parse(expected), Assert.IsType<ISolidColorBrush>(value, exactMatch: false).Color);
    });
}
