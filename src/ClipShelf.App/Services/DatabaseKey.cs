using System.Security.Cryptography;
using System.Text;

namespace ClipShelf.App.Services;

// The SQLCipher key for the history database, protected with Windows DPAPI for the current user.
// Deleting the file makes the existing history unreadable, so it is backed up with the database.
public static class DatabaseKey
{
    public static string LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            var blob = File.ReadAllBytes(path);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(blob, null, DataProtectionScope.CurrentUser));
        }

        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var protectedBlob = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(path, protectedBlob);
        return key;
    }
}
