using Avalonia.Media;
using Copyloft.Desktop.Controls;
using Copyloft.Desktop.Localization;
using Copyloft.Desktop.Resources;

namespace Copyloft.Desktop.Tests;

public sealed class IconAndTrTests
{
    [Fact]
    public Task Every_icon_parses_to_a_geometry() => Ui.Run(() =>
    {
        foreach (var kind in Enum.GetValues<IconKind>())
            Assert.NotNull(StreamGeometry.Parse(IconData.Get(kind)));
    });

    [Fact]
    public Task An_app_icon_draws_its_kind() => Ui.Run(() =>
    {
        var icon = new AppIcon { Kind = IconKind.Settings };

        Assert.NotNull(icon.Data);
    });

    [Fact]
    public void The_tr_extension_returns_the_resource_text() =>
        Assert.Equal("Copyloft", new TrExtension { Key = "App_Name" }.ProvideValue(null!));
}
