using System.Runtime.CompilerServices;

namespace ClipShelf.Data.Tests;

// The SQLCipher bundle must be initialized before any connection is opened.
internal static class TestSqlite
{
    [ModuleInitializer]
    internal static void Init() => SQLitePCL.Batteries_V2.Init();
}
