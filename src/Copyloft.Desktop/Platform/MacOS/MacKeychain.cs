using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using static Copyloft.Desktop.Platform.MacOS.MacNative;

namespace Copyloft.Desktop.Platform.MacOS;

// The database key as a generic password in the login Keychain. Any status other than "found" or
// "not found" (a locked or missing keychain) reads as "no keyring", never a throw.
[SupportedOSPlatform("macos")]
public sealed class MacKeychain(ILogger<MacKeychain> log, string service = "Copyloft") : ISecretKeyring
{
    private readonly byte[] _service = Encoding.UTF8.GetBytes(service);
    private static readonly byte[] Account = Encoding.UTF8.GetBytes("database-key");

    public bool TryLookup(out string? secret)
    {
        secret = null;
        var status = SecKeychainFindGenericPassword(
            IntPtr.Zero, (uint)_service.Length, _service, (uint)Account.Length, Account,
            out var length, out var data, IntPtr.Zero);
        if (status == ErrSecItemNotFound) return true;
        if (status != 0)
        {
            log.LogWarning("Reading the database key from the Keychain failed: OSStatus {Status}", status);
            return false;
        }

        try
        {
            secret = Marshal.PtrToStringUTF8(data, (int)length);
            return true;
        }
        finally
        {
            SecKeychainItemFreeContent(IntPtr.Zero, data);
        }
    }

    public bool TryStore(string secret)
    {
        var password = Encoding.UTF8.GetBytes(secret);
        var status = SecKeychainAddGenericPassword(
            IntPtr.Zero, (uint)_service.Length, _service, (uint)Account.Length, Account,
            (uint)password.Length, password, IntPtr.Zero);
        if (status != 0) log.LogWarning("Saving the database key to the Keychain failed: OSStatus {Status}", status);
        return status == 0;
    }
}
