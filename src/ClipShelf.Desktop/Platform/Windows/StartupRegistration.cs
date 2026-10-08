using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ClipShelf.Desktop.Platform.Windows;

// Writes the executable path to the per-user Run key so Windows starts ClipShelf at sign-in.
// Applying every launch also repairs the path after the app moves (for example after an update).
[SupportedOSPlatform("windows")]
public sealed class StartupRegistration(ILogger<StartupRegistration> log) : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClipShelf";

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
