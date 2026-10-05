using System.Globalization;
using System.Reflection;
using System.Resources;
using ClipShelf.App.Localization;

namespace ClipShelf.App.Tests;

// Fails when the neutral (English) and Arabic resx files drift apart or code uses a missing key.
public sealed class LocalizationTests
{
    private static readonly string[] PluralForms = ["zero", "one", "two", "few", "many", "other"];
    private static readonly Regex KeyUsage = new(
        @"\{l:Tr\s+(?:Key=)?(?<k>\w+)\}|Tr\.(?<m>Get|Format|Plural)\(""(?<k>\w+)""",
        RegexOptions.Compiled);

    [Fact]
    public void Neutral_and_Arabic_files_have_identical_keys()
    {
        var neutral = Keys("Strings.resx");
        var arabic = Keys("Strings.ar.resx");
        Assert.Empty(neutral.Except(arabic));
        Assert.Empty(arabic.Except(neutral));
    }

    [Fact]
    public void Neutral_language_is_English()
    {
        var attribute = typeof(ClipShelf.App.Hosting.Culture).Assembly
            .GetCustomAttribute<NeutralResourcesLanguageAttribute>();
        Assert.NotNull(attribute);
        Assert.StartsWith("en", attribute!.CultureName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Arabic_file_holds_arabic_text()
    {
        var values = Values("Strings.ar.resx");
        Assert.True(
            values.Count(IsArabic) > 100,
            "The Arabic file should hold Arabic text, not copies of the English values.");
    }

    [Fact]
    public void Every_key_used_in_code_exists()
    {
        var keys = Keys("Strings.resx");
        Assert.Empty(UsedKeys().Where(k => !keys.Contains(k)).Distinct());
    }

    [Fact]
    public void Plural_follows_the_culture_rules()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar-SA");
            Assert.Equal(Tr.Format("History_Count_zero", 0), Tr.Plural("History_Count", 0));
            Assert.Equal(Tr.Format("History_Count_one", 1), Tr.Plural("History_Count", 1));
            Assert.Equal(Tr.Format("History_Count_two", 2), Tr.Plural("History_Count", 2));
            Assert.Equal(Tr.Format("History_Count_few", 5), Tr.Plural("History_Count", 5));
            Assert.Equal(Tr.Format("History_Count_many", 42), Tr.Plural("History_Count", 42));
            Assert.Equal(Tr.Format("History_Count_other", 100), Tr.Plural("History_Count", 100));

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Equal(Tr.Format("History_Count_one", 1), Tr.Plural("History_Count", 1));
            Assert.Equal(Tr.Format("History_Count_other", 5), Tr.Plural("History_Count", 5));
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    private static IEnumerable<string> UsedKeys()
    {
        foreach (var file in TestPaths.SourceFiles(".xaml", ".cs"))
        foreach (Match match in KeyUsage.Matches(File.ReadAllText(file)))
        {
            var key = match.Groups["k"].Value;
            if (match.Groups["m"].Value == "Plural")
                foreach (var form in PluralForms) yield return $"{key}_{form}";
            else
                yield return key;
        }
    }

    private static bool IsArabic(string value) => value.Any(c => c is >= '\u0600' and <= '\u06FF');

    private static HashSet<string> Keys(string fileName) =>
        XDocument.Load(Path.Combine(TestPaths.AppProject, "Resources", fileName))
            .Root!.Elements("data")
            .Select(d => (string)d.Attribute("name")!)
            .ToHashSet(StringComparer.Ordinal);

    private static List<string> Values(string fileName) =>
        XDocument.Load(Path.Combine(TestPaths.AppProject, "Resources", fileName))
            .Root!.Elements("data")
            .Select(d => (string?)d.Element("value") ?? string.Empty)
            .ToList();
}
