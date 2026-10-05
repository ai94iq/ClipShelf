using System.Resources;

namespace ClipShelf.App.Localization;

// The only way to get user-visible text. Keys live in Resources/Strings.resx (English, neutral) and Strings.ar.resx.
public static class Tr
{
    // RootNamespace + folder + file name. There is deliberately no generated Designer.cs.
    private static readonly ResourceManager Strings =
        new("ClipShelf.App.Resources.Strings", typeof(Tr).Assembly);

    public static string Get(string key) =>
        Strings.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    // Six-key convention (zero/one/two/few/many/other): English uses zero/one/other,
    // Arabic uses all six (CLDR: 0, 1, 2, 3-10, 11-99, 100+).
    public static string Plural(string key, int count)
    {
        var form = Form(count, CultureInfo.CurrentUICulture);
        return Format($"{key}_{form}", count);
    }

    private static string Form(int count, CultureInfo culture)
    {
        if (culture.TwoLetterISOLanguageName == "ar")
        {
            return count switch
            {
                0 => "zero",
                1 => "one",
                2 => "two",
                <= 10 => "few",
                <= 99 => "many",
                _ => "other",
            };
        }

        return count switch
        {
            0 => "zero",
            1 => "one",
            _ => "other",
        };
    }
}
