namespace ClipShelf.Desktop.Platform.Linux;

// One secret in the user's keyring. Both calls return false when no keyring service answers;
// TryLookup returns true with a null secret when the keyring answers but holds nothing.
public interface ISecretKeyring
{
    bool TryLookup(out string? secret);
    bool TryStore(string secret);
}
