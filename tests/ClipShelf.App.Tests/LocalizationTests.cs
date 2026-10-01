namespace ClipShelf.App.Tests;

// Fails when Arabic and English resx files drift apart or code uses a missing key.
public sealed class LocalizationTests
{
    private static readonly string[] PluralForms = ["zero", "one", "two", "few", "many", "other"];
    private static readonly Regex KeyUsage = new(
        @"\{l:Tr\s+(?:Key=)?(?<k>\w+)\}|Tr\.(?<m>Get|Format|Plural)\(""(?<k>\w+)""",
        RegexOptions.Compiled);

    [Fact]
    public void Arabic_and_English_files_have_identical_keys()
    {
        var arabic = Keys("Strings.resx");
        var english = Keys("Strings.en.resx");
        Assert.Empty(arabic.Except(english));
        Assert.Empty(english.Except(arabic));
    }

    [Fact]
    public void Every_key_used_in_code_exists()
    {
        var keys = Keys("Strings.resx");
        Assert.Empty(UsedKeys().Where(k => !keys.Contains(k)).Distinct());
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

    private static HashSet<string> Keys(string fileName) =>
        XDocument.Load(Path.Combine(TestPaths.AppProject, "Resources", fileName))
            .Root!.Elements("data")
            .Select(d => (string)d.Attribute("name")!)
            .ToHashSet(StringComparer.Ordinal);
}
