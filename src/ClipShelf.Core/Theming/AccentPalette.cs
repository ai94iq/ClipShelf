namespace ClipShelf.Core.Theming;

// Derives the six Windows accent shades from one base color by mixing with white or black.
public static class AccentPalette
{
    public static AccentShades Shades(string hex)
    {
        var (r, g, b) = Parse(hex);
        return new AccentShades(
            Base: hex.ToUpperInvariant(),
            Light1: Mix(r, g, b, 255, 0.2), Light2: Mix(r, g, b, 255, 0.4), Light3: Mix(r, g, b, 255, 0.6),
            Dark1: Mix(r, g, b, 0, 0.2), Dark2: Mix(r, g, b, 0, 0.4), Dark3: Mix(r, g, b, 0, 0.6));
    }

    public static (byte R, byte G, byte B) Parse(string hex)
    {
        if (hex.Length != 7 || hex[0] != '#') throw new FormatException($"Expected #RRGGBB, got '{hex}'.");
        var value = Convert.ToInt32(hex[1..], 16);
        return ((byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }

    private static string Mix(byte r, byte g, byte b, byte target, double amount)
    {
        byte Blend(byte channel) => (byte)Math.Round(channel + ((target - channel) * amount));
        return string.Create(CultureInfo.InvariantCulture, $"#{Blend(r):X2}{Blend(g):X2}{Blend(b):X2}");
    }
}
