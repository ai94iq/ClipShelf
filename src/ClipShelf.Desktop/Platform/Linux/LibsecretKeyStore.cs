using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace ClipShelf.Desktop.Platform.Linux;

// The SQLCipher key for the history database, kept in the freedesktop Secret
// Service (GNOME Keyring, KDE Wallet) under org.clipshelf.Database. Native
// entry points resolve lazily so construction never touches libsecret; a box
// without it fails on first use with a clear message instead of at startup.
//
// Layout notes (libsecret secret-schema.h, glib): SecretSchema is name ptr,
// flags int, then 32 attribute slots of name ptr plus type int; attribute
// types are STRING 0, INTEGER 1, BOOLEAN 2. HGlobal strings are malloc-backed
// on Unix, so the table owns them through g_free.
[SupportedOSPlatform("linux")]
public sealed class LibsecretKeyStore : IKeyStore
{
    private const string SchemaName = "org.clipshelf.Database";
    private const string ApplicationValue = "clipshelf";
    private const string AccountValue = "database";
    private const int SchemaSize = 528;

    public string LoadOrCreate(string path)
    {
        _ = path;   // The key lives in the keyring, not in a file.
        try
        {
            if (Lookup() is { } existing) return existing;
        }
        catch (DllNotFoundException ex)
        {
            throw new InvalidOperationException(
                "ClipShelf needs libsecret (GNOME Keyring or KDE Wallet) on Linux.", ex);
        }

        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        Store(key);
        return key;
    }

    private static string? Lookup()
    {
        var table = BuildAttributes(out var schema);
        try
        {
            var password = Native.Lookup(schema, table, IntPtr.Zero, out var error);
            using (error)
            {
                if (password == IntPtr.Zero)
                {
                    error.ThrowIfFailed("reading the database key from the keyring");
                    return null;
                }

                try
                {
                    return Marshal.PtrToStringUTF8(password);
                }
                finally
                {
                    Native.FreePassword(password);
                }
            }
        }
        finally
        {
            Native.UnrefTable(table);
            FreeSchema(schema);
        }
    }

    private static void Store(string key)
    {
        var table = BuildAttributes(out var schema);
        try
        {
            var ok = Native.Store(
                schema, table, IntPtr.Zero, "ClipShelf database key", key, IntPtr.Zero, out var error);
            using (error)
            {
                if (!ok) error.ThrowIfFailed("saving the database key to the keyring");
            }
        }
        finally
        {
            Native.UnrefTable(table);
            FreeSchema(schema);
        }
    }

    // Builds the schema block and the attribute table. The table takes ownership
    // of the key and value strings through g_free; the schema block is freed by
    // the caller.
    private static IntPtr BuildAttributes(out IntPtr schema)
    {
        var glib = NativeLibrary.Load("libglib-2.0.so.0");
        var strHash = NativeLibrary.GetExport(glib, "g_str_hash");
        var strEqual = NativeLibrary.GetExport(glib, "g_str_equal");
        var free = NativeLibrary.GetExport(glib, "g_free");

        schema = Marshal.AllocHGlobal(SchemaSize);
        for (var i = 0; i < SchemaSize / IntPtr.Size; i++)
            Marshal.WriteIntPtr(schema, i * IntPtr.Size, IntPtr.Zero);
        Marshal.WriteIntPtr(schema, 0, Marshal.StringToHGlobalAnsi(SchemaName));
        Marshal.WriteInt32(schema, IntPtr.Size, 0);
        WriteAttribute(schema, 0, "application", ApplicationValue);
        WriteAttribute(schema, 1, "account", AccountValue);

        var table = Native.NewTable(strHash, strEqual, free, free);
        Native.Insert(table, Marshal.StringToHGlobalAnsi("application"), Marshal.StringToHGlobalAnsi(ApplicationValue));
        Native.Insert(table, Marshal.StringToHGlobalAnsi("account"), Marshal.StringToHGlobalAnsi(AccountValue));
        return table;
    }

    private static void WriteAttribute(IntPtr schema, int index, string name, string value)
    {
        _ = value;
        var slot = schema + IntPtr.Size + sizeof(int) + IntPtr.Size + (index * IntPtr.Size * 2);
        Marshal.WriteIntPtr(slot, Marshal.StringToHGlobalAnsi(name));
        Marshal.WriteInt32(slot, IntPtr.Size, 0);
    }

    private static void FreeSchema(IntPtr schema)
    {
        Marshal.FreeHGlobal(Marshal.ReadIntPtr(schema, 0));
        for (var i = 0; i < 2; i++)
        {
            var slot = schema + IntPtr.Size + sizeof(int) + IntPtr.Size + (i * IntPtr.Size * 2);
            Marshal.FreeHGlobal(Marshal.ReadIntPtr(slot));
        }

        Marshal.FreeHGlobal(schema);
    }

    private static class Native
    {
        [DllImport("libsecret-1.so.0")]
        internal static extern IntPtr secret_password_lookup_sync(
            IntPtr schema, IntPtr attributes, IntPtr cancellable, out NativeError error);

        [DllImport("libsecret-1.so.0", CharSet = CharSet.Ansi)]
        internal static extern int secret_password_store_sync(
            IntPtr schema, IntPtr attributes, IntPtr collection, string label,
            string password, IntPtr cancellable, out NativeError error);

        [DllImport("libsecret-1.so.0")]
        internal static extern void secret_password_free(IntPtr password);

        [DllImport("libglib-2.0.so.0")]
        internal static extern IntPtr g_hash_table_new_full(
            IntPtr hashFunc, IntPtr keyEqualFunc, IntPtr keyDestroyFunc, IntPtr valueDestroyFunc);

        [DllImport("libglib-2.0.so.0")]
        internal static extern void g_hash_table_insert(IntPtr table, IntPtr key, IntPtr value);

        [DllImport("libglib-2.0.so.0")]
        internal static extern void g_hash_table_unref(IntPtr table);

        [DllImport("libglib-2.0.so.0")]
        internal static extern void g_error_free(IntPtr error);

        internal static IntPtr Lookup(IntPtr schema, IntPtr table, IntPtr cancellable, out NativeError error) =>
            secret_password_lookup_sync(schema, table, cancellable, out error);

        internal static bool Store(
            IntPtr schema, IntPtr table, IntPtr collection, string label,
            string password, IntPtr cancellable, out NativeError error) =>
            secret_password_store_sync(schema, table, collection, label, password, cancellable, out error) != 0;

        internal static void FreePassword(IntPtr password) => secret_password_free(password);

        internal static IntPtr NewTable(IntPtr hash, IntPtr equal, IntPtr keyFree, IntPtr valueFree) =>
            g_hash_table_new_full(hash, equal, keyFree, valueFree);

        internal static void Insert(IntPtr table, IntPtr key, IntPtr value) =>
            g_hash_table_insert(table, key, value);

        internal static void UnrefTable(IntPtr table) => g_hash_table_unref(table);

        internal readonly struct NativeError : IDisposable
        {
            private readonly IntPtr _handle;

            public void ThrowIfFailed(string action)
            {
                if (_handle == IntPtr.Zero)
                    throw new InvalidOperationException($"ClipShelf failed {action} (no detail).");

                var message = Marshal.PtrToStringUTF8(_handle + 8) ?? "unknown error";
                throw new InvalidOperationException($"ClipShelf failed {action}: {message}");
            }

            public void Dispose()
            {
                if (_handle != IntPtr.Zero) g_error_free(_handle);
            }
        }
    }
}
