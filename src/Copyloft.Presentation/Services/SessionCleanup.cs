namespace Copyloft.Presentation.Services;

// Clears the unpinned history when the OS ends the session, if the setting is on.
public sealed class SessionCleanup
{
    public SessionCleanup(
        ISessionEnd sessionEnd, SettingsService settings, IClipRepository repository, ICategoryLockService locks)
    {
        sessionEnd.Ending += (_, _) =>
        {
            if (settings.Current.LockOnShutdown) locks.Reset();
            if (settings.Current.ClearOnSignOut) repository.ClearUnpinned();
        };
    }
}
