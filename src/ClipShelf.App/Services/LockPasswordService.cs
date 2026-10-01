using ClipShelf.Core.Security;

namespace ClipShelf.App.Services;

// Keeps the password in settings.json as a salted PBKDF2 hash; never the plain text.
public sealed class LockPasswordService(SettingsService settings) : ILockPasswordService
{
    public bool IsSet => settings.Current.LockPasswordHash is not null;

    public bool Verify(string password) =>
        PasswordHash.Verify(password, settings.Current.LockPasswordHash ?? string.Empty);

    public void Set(string password) =>
        settings.Update(settings.Current with { LockPasswordHash = PasswordHash.Create(password) });

    public void Remove() =>
        settings.Update(settings.Current with { LockPasswordHash = null });
}
