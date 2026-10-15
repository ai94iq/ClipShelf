using Copyloft.Desktop.Platform;

namespace Copyloft.Desktop.Tests;

// An install upgraded from ClipShelf keeps its database key; a missing keyring stays "no keyring".
public sealed class LegacyKeyringTests
{
    [Fact]
    public void A_ClipShelf_key_is_returned_and_copied_to_the_Copyloft_entry()
    {
        var current = new Keyring();
        var legacy = new Keyring { Secret = "old-key" };

        Assert.True(new LegacyKeyring(current, legacy).TryLookup(out var secret));

        Assert.Equal("old-key", secret);
        Assert.Equal("old-key", current.Secret);
        Assert.Equal("old-key", legacy.Secret);
    }

    [Fact]
    public void A_Copyloft_key_wins_over_the_ClipShelf_key()
    {
        var keyring = new LegacyKeyring(new Keyring { Secret = "new-key" }, new Keyring { Secret = "old-key" });

        Assert.True(keyring.TryLookup(out var secret));
        Assert.Equal("new-key", secret);
    }

    [Fact]
    public void With_neither_key_the_keyring_answers_empty()
    {
        Assert.True(new LegacyKeyring(new Keyring(), new Keyring()).TryLookup(out var secret));
        Assert.Null(secret);
    }

    [Fact]
    public void An_unavailable_keyring_reads_as_no_keyring()
    {
        var keyring = new LegacyKeyring(new Keyring { Available = false }, new Keyring { Secret = "old-key" });

        Assert.False(keyring.TryLookup(out _));
    }

    private sealed class Keyring : ISecretKeyring
    {
        public bool Available { get; init; } = true;
        public string? Secret { get; set; }

        public bool TryLookup(out string? secret)
        {
            secret = Available ? Secret : null;
            return Available;
        }

        public bool TryStore(string secret)
        {
            if (Available) Secret = secret;
            return Available;
        }
    }
}
