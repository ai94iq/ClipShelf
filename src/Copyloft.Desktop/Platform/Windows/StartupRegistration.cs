using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Copyloft.Desktop.Platform.Windows;

// Writes the executable path to the per-user Run key so Windows starts Copyloft at sign-in.
// Applying every launch also repairs the path after the app moves (for example after an update).
[SupportedOSPlatform("windows")]
public sealed class StartupRegistration(ILogger<StartupRegistration> log) : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Copyloft";
    private const string LegacyValueName = "ClipShelf";   // the app's name up to 0.4.1, removed on every apply

    public void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                log.LogWarning("The Run key is unavailable; the startup setting was not applied");
                return;
            }

            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
            if (enabled)
                key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Applying the startup setting failed");
        }
    }
}
