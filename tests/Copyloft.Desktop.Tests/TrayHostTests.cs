using Copyloft.Desktop.Platform;

namespace Copyloft.Desktop.Tests;

public sealed class TrayHostTests
{
    [Theory]
    [InlineData("method return time=1.2 sender=org.freedesktop.DBus -> destination=:1.9 serial=3 reply_serial=2\n   boolean true\n", true)]
    [InlineData("method return time=1.2 sender=org.freedesktop.DBus -> destination=:1.9 serial=3 reply_serial=2\n   boolean false\n", false)]
    [InlineData("", false)]
    public void The_watcher_answer_is_read_from_dbus_send(string output, bool available) =>
        Assert.Equal(available, TrayHost.HasOwner(output));

    [Fact]
    public void Windows_always_has_a_notification_area()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows");

        Assert.True(TrayHost.IsAvailable());
    }

    // WSLg and Xvfb run no StatusNotifierWatcher, like GNOME without the AppIndicator extension.
    [Fact]
    public void A_linux_session_without_a_watcher_has_no_tray()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") is null or "", "a Linux session without a desktop shell");

        Assert.False(TrayHost.IsAvailable());
    }
}
