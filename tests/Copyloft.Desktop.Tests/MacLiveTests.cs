using System.Diagnostics;
using System.Runtime.Versioning;
using Copyloft.Core.Input;
using Copyloft.Desktop.Platform.MacOS;
using Microsoft.Extensions.Logging.Abstractions;

namespace Copyloft.Desktop.Tests;

// Against the real macOS APIs; they run on the macOS CI runner (no Mac here). The Keychain test
// uses its own service name and deletes it, so a developer's Copyloft entry is never touched.
[SupportedOSPlatform("macos")]
public sealed class MacLiveTests
{
    private static void SkipOffMac() => Assert.SkipUnless(OperatingSystem.IsMacOS(), "macOS");

    private static int Run(string file, params string[] args)
    {
        using var process = Process.Start(new ProcessStartInfo(file, args) { RedirectStandardInput = true })!;
        process.StandardInput.Close();
        process.WaitForExit();
        return process.ExitCode;
    }

    [Fact]
    public void The_keychain_keeps_a_secret_and_reports_a_missing_one()
    {
        SkipOffMac();
        var service = "Copyloft.Test." + Guid.NewGuid().ToString("N");
        var keychain = new MacKeychain(NullLogger<MacKeychain>.Instance, service);
        try
        {
            Assert.True(keychain.TryLookup(out var missing));
            Assert.Null(missing);

            Assert.True(keychain.TryStore("0123ABCD"));
            Assert.True(keychain.TryLookup(out var found));
            Assert.Equal("0123ABCD", found);
        }
        finally
        {
            Run("security", "delete-generic-password", "-s", service);
        }
    }

    [Fact]
    public void A_copy_moves_the_pasteboard_change_count()
    {
        SkipOffMac();
        using var watcher = new PasteboardWatcher();
        var before = watcher.ChangeCount();

        using (var copy = Process.Start(new ProcessStartInfo("pbcopy") { RedirectStandardInput = true })!)
        {
            copy.StandardInput.Write("Copyloft test");
            copy.StandardInput.Close();
            copy.WaitForExit();
        }

        Assert.NotEqual(before, watcher.ChangeCount());
    }

    [Fact]
    public void The_frontmost_app_and_the_pointer_answer()
    {
        SkipOffMac();
        var desktop = new MacDesktop();

        _ = desktop.Name();
        _ = desktop.ForegroundWindow();
        var pointer = desktop.Cursor();

        Assert.True(pointer.X >= 0 && pointer.Y >= 0);
    }

    [Fact]
    public void A_carbon_hotkey_registers()
    {
        SkipOffMac();
        using var hotkey = new MacGlobalHotkey(NullLogger<MacGlobalHotkey>.Instance);

        Assert.True(hotkey.Register(new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x82))); // F19
    }
}
