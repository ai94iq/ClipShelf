using System.Security.Cryptography;

namespace ClipShelf.Desktop.Platform;

// The SQLCipher key for the history database, kept in the system keyring (Secret Service on Linux,
// the Keychain on macOS). The file at the key path always exists once a key does: it holds the
// marker when the keyring has the key, or the key itself (0600) when no keyring was there to take
// it. A marked key the keyring cannot give back is an error, never a new key: a new key would make
// the existing history unreadable.
public sealed class KeyringKeyStore(ISecretKeyring keyring, ILogger<KeyringKeyStore> log) : IKeyStore
{
    internal const string KeyringMarker = "keyring";

    public string LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            var stored = File.ReadAllText(path).Trim();
            if (stored != KeyringMarker)
                return stored;
            if (keyring.TryLookup(out var secret) && secret is not null)
                return secret;
            throw new KeyringUnavailableException(
                "The database key is in the system keyring, which did not return it. Unlock the keyring and start ClipShelf again.");
        }

        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        if (keyring.TryStore(key))
        {
            File.WriteAllText(path, KeyringMarker);
            return key;
        }

        log.LogWarning("No system keyring; the database key is kept in {Path}, readable only by this user", path);
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var file = new FileStream(path, options))
        using (var writer = new StreamWriter(file))
            writer.Write(key);
        return key;
    }
}

// Locked, or the user denied the request; startup says so instead of only logging it.
public sealed class KeyringUnavailableException(string message) : InvalidOperationException(message);
