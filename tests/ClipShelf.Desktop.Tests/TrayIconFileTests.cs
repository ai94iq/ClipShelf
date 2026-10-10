using ClipShelf.Core.Theming;
using ClipShelf.Desktop.Platform.Windows;

namespace ClipShelf.Desktop.Tests;

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
    public void Every_resolved_file_exists_in_the_assets()
    {
        var assets = Path.Combine(TestPaths.DesktopProject, "Assets");

        foreach (var kind in Enum.GetValues<TrayIconKind>())
        foreach (var taskbarIsLight in new[] { true, false })
        {
            var file = TrayIconFiles.FileName(kind, taskbarIsLight);
            Assert.True(File.Exists(Path.Combine(assets, file)), file);
        }
    }
}
