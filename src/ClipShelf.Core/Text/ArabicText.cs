namespace ClipShelf.Core.Text;

public static class ArabicText
{
    // Normalizes text for search: removes tashkeel and tatweel, unifies alef/yaa/taa marbuta forms,
    // lowercases Latin. Store the result in a *Search column and normalize queries the same way.
    public static string NormalizeForSearch(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text.Trim())
        {
            switch (ch)
            {
                case >= '\u064B' and <= '\u065F':   // harakat / tashkeel
                case '\u0670':                      // superscript alef
                case '\u0640':                      // tatweel
                    continue;
                case 'أ' or 'إ' or 'آ' or 'ٱ': sb.Append('ا'); break;
                case 'ى': sb.Append('ي'); break;
                case 'ؤ': sb.Append('و'); break;
                case 'ئ': sb.Append('ي'); break;
                case 'ة': sb.Append('ه'); break;
                default: sb.Append(char.ToLowerInvariant(ch)); break;
            }
        }
        return sb.ToString();
    }
}
