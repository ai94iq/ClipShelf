using ClipShelf.App.Platform;

namespace ClipShelf.App.Services;

// Clears the unpinned history when Windows ends the session, if the setting is on.
internal sealed class SessionCleanup
{
    public SessionCleanup(
        SessionWatcher watcher, SettingsService settings, IClipRepository repository, ICategoryLockService locks)
    {
        watcher.Ending += (_, _) =>
        {
            if (settings.Current.LockOnShutdown) locks.Reset();
            if (settings.Current.ClearOnSignOut) repository.ClearUnpinned();
        };
    }
}
