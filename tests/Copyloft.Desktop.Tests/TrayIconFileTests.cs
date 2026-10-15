using Copyloft.Core.Theming;
using Copyloft.Desktop.Platform;

namespace Copyloft.Desktop.Tests;

public sealed class TrayIconFileTests
{
    [Theory]
    [InlineData(TrayIconKind.Filled, true, "tray-light.ico")]
    [InlineData(TrayIconKind.Filled, false, "tray-dark.ico")]
    [InlineData(TrayIconKind.Outline, true, "tray-outline-light.ico")]
    [InlineData(TrayIconKind.Outline, false, "tray-outline-dark.ico")]
    public void The_file_follows_the_style_and_the_taskbar_theme(
        TrayIconKind kind, bool taskbarIsLight, string expected) =>
        Assert.Equal(expected, TrayIconFiles.FileName(kind, taskbarIsLight));

    [Fact]
    public void Every_resolved_file_ships_next_to_the_app()
    {
        foreach (var kind in Enum.GetValues<TrayIconKind>())
        foreach (var taskbarIsLight in new[] { true, false })
        {
            var file = TrayIconFiles.FileName(kind, taskbarIsLight);
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Assets", file)), file);
        }
    }
}
