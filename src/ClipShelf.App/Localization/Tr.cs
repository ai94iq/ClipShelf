using System.Resources;

namespace ClipShelf.App.Localization;

// The only way to get user-visible text. Keys live in Resources/Strings.resx (ar) and Strings.en.resx.
public static class Tr
{
    // RootNamespace + folder + file name. There is deliberately no generated Designer.cs.
    private static readonly ResourceManager Strings =
        new("ClipShelf.App.Resources.Strings", typeof(Tr).Assembly);

    public static string Get(string key) =>
        Strings.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    // Uses keys {baseKey}_zero/_one/_two/_few/_many/_other, which must exist in both resx files.
    public static string Plural(string baseKey, long count) =>
        Format($"{baseKey}_{ArabicPlural.Form(count)}", count);
}
