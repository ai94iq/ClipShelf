namespace Copyloft.Desktop.Platform;

// The app was ClipShelf up to 0.4.1, and its database key sits in the keyring under that name. When
// the Copyloft entry is empty, the key is read from the ClipShelf entry and copied over. The old
// entry stays, so a failed copy only means the next start reads it again.
public sealed class LegacyKeyring(ISecretKeyring current, ISecretKeyring legacy) : ISecretKeyring
{
    public bool TryLookup(out string? secret)
    {
        if (!current.TryLookup(out secret))
            return false;
        if (secret is not null)
            return true;
        if (legacy.TryLookup(out secret) && secret is not null)
            current.TryStore(secret);
        return true;
    }

    public bool TryStore(string secret) => current.TryStore(secret);
}
