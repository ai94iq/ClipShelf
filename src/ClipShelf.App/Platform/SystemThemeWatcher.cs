namespace ClipShelf.App.Platform;

// Reports the moment Windows writes the taskbar theme value. The WM_SETTINGCHANGE broadcast that
// announces the same change is sent to every top-level window in turn, so it can reach this app's
// hidden window after the taskbar has already repainted; a registry notification has no such
// queue. The watch runs on a pool thread and Changed fires there.
internal sealed class SystemThemeWatcher : IDisposable
{
    private readonly AutoResetEvent _changed = new(false);
    private readonly RegisteredWaitHandle? _wait;
    private readonly IntPtr _key;
    private bool _disposed;

    public SystemThemeWatcher()
    {
        if (NativeMethods.RegOpenKeyEx(
                NativeMethods.HkeyCurrentUser, SystemTheme.PersonalizeKey, 0,
                NativeMethods.KeyNotify, out _key) != 0)
        {
            return;     // the WM_SETTINGCHANGE fallback still reports theme changes
        }

        Arm();
        _wait = ThreadPool.RegisterWaitForSingleObject(
            _changed, (_, _) => OnChanged(), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    // Raised when the key changes (which covers AppsUseLightTheme writes too, so readers still
    // have to compare the value they care about).
    public event Action? Changed;

    public void Dispose()
    {
        _disposed = true;
        _wait?.Unregister(null);
        if (_key != IntPtr.Zero) NativeMethods.RegCloseKey(_key);
        _changed.Dispose();
    }

    private void OnChanged()
    {
        if (_disposed) return;

        Arm();
        Changed?.Invoke();
    }

    private void Arm() =>
        _ = NativeMethods.RegNotifyChangeKeyValue(
            _key, watchSubtree: false, NativeMethods.NotifyChangeLastSet,
            _changed.SafeWaitHandle.DangerousGetHandle(), asynchronous: true);
}
