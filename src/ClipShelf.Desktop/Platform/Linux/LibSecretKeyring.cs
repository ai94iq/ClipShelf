using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#pragma warning disable SYSLIB1054 // DllImport is intentional: the app ships self-contained, not Native AOT.

namespace ClipShelf.Desktop.Platform.Linux;

// The Secret Service (GNOME Keyring, KWallet) through libsecret's non-variadic calls. A missing
// library or a session without a keyring service reads as "no keyring" (false), never a throw.
[SupportedOSPlatform("linux")]
public sealed class LibSecretKeyring(ILogger<LibSecretKeyring> log) : ISecretKeyring
{
    private const string Secret = "libsecret-1.so.0";
    private const string Glib = "libglib-2.0.so.0";
    private const string SchemaName = "io.github.ai94iq.ClipShelf";
    private const string AttributeName = "purpose";
    private const string AttributeValue = "database-key";

    public bool TryLookup(out string? secret)
    {
        secret = null;
        string? found = null;
        var ok = Call((schema, attributes) =>
        {
            var error = IntPtr.Zero;
            var value = secret_password_lookupv_sync(schema, attributes, IntPtr.Zero, ref error);
            if (value != IntPtr.Zero)
            {
                found = Marshal.PtrToStringUTF8(value);
                secret_password_free(value);
            }
            return Report(error, "Reading the database key from the keyring");
        });
        secret = found;
        return ok;
    }

    public bool TryStore(string secret) => Call((schema, attributes) =>
    {
        var error = IntPtr.Zero;
        var stored = secret_password_storev_sync(
            schema, attributes, null, "Copyloft database key", secret, IntPtr.Zero, ref error);
        return Report(error, "Saving the database key to the keyring") && stored;
    });

    // Builds the schema and the attribute table, runs the call, frees everything.
    private bool Call(Func<IntPtr, IntPtr, bool> call)
    {
        var name = Marshal.StringToCoTaskMemUTF8(AttributeName);
        var value = Marshal.StringToCoTaskMemUTF8(AttributeValue);
        IntPtr types = IntPtr.Zero, attributes = IntPtr.Zero, schema = IntPtr.Zero;
        try
        {
            var glib = NativeLibrary.Load(Glib);
            var hash = NativeLibrary.GetExport(glib, "g_str_hash");
            var equal = NativeLibrary.GetExport(glib, "g_str_equal");

            types = g_hash_table_new(hash, equal);
            g_hash_table_insert(types, name, IntPtr.Zero); // SECRET_SCHEMA_ATTRIBUTE_STRING
            schema = secret_schema_newv(SchemaName, 0, types);

            attributes = g_hash_table_new(hash, equal);
            g_hash_table_insert(attributes, name, value);
            return call(schema, attributes);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            log.LogWarning("libsecret is not installed: {Message}", ex.Message);
            return false;
        }
        finally
        {
            if (schema != IntPtr.Zero) secret_schema_unref(schema);
            if (attributes != IntPtr.Zero) g_hash_table_unref(attributes);
            if (types != IntPtr.Zero) g_hash_table_unref(types);
            Marshal.FreeCoTaskMem(name);
            Marshal.FreeCoTaskMem(value);
        }
    }

    // GError: guint32 domain, gint code, gchar* message.
    private bool Report(IntPtr error, string what)
    {
        if (error == IntPtr.Zero)
            return true;
        log.LogWarning("{What} failed: {Message}", what, Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(error, 8)));
        g_error_free(error);
        return false;
    }

    [DllImport(Secret)]
    private static extern IntPtr secret_schema_newv(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int flags, IntPtr attributeNamesAndTypes);

    [DllImport(Secret)]
    private static extern void secret_schema_unref(IntPtr schema);

    [DllImport(Secret)]
    private static extern IntPtr secret_password_lookupv_sync(
        IntPtr schema, IntPtr attributes, IntPtr cancellable, ref IntPtr error);

    [DllImport(Secret)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool secret_password_storev_sync(
        IntPtr schema,
        IntPtr attributes,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? collection,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string password,
        IntPtr cancellable,
        ref IntPtr error);

    [DllImport(Secret)]
    private static extern void secret_password_free(IntPtr password);

    [DllImport(Glib)]
    private static extern IntPtr g_hash_table_new(IntPtr hash, IntPtr equal);

    [DllImport(Glib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool g_hash_table_insert(IntPtr table, IntPtr key, IntPtr value);

    [DllImport(Glib)]
    private static extern void g_hash_table_unref(IntPtr table);

    [DllImport(Glib)]
    private static extern void g_error_free(IntPtr error);
}
