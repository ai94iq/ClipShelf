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

    // English plural forms: zero, one, other. The six-key convention (zero/one/two/few/many/other)
    // keeps the resx ready for a locale with richer rules.
    public static string Plural(string key, int count)
    {
        var form = count switch
        {
            0 => "zero",
            1 => "one",
            _ => "other",
        };

        return Format($"{key}_{form}", count);
    }
}
