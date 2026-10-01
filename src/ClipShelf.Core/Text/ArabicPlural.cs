namespace ClipShelf.Core.Text;

// CLDR Arabic plural categories. English resx files fill all six keys too.
public static class ArabicPlural
{
    public static string Form(long n) => (n % 100) switch
    {
        _ when n == 0 => "zero",
        _ when n == 1 => "one",
        _ when n == 2 => "two",
        >= 3 and <= 10 => "few",
        >= 11 and <= 99 => "many",
        _ => "other",
    };
}
