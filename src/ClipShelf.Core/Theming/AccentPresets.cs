namespace ClipShelf.Core.Theming;

// Curated pastel accents: soft, modern tones. The display name comes from resx key
// "Accent_{Key}". Null accent means "use the Windows accent".
public static class AccentPresets
{
    public static IReadOnlyList<AccentPreset> All { get; } =
    [
        new("Blue", "#A8C7FA"),
        new("Teal", "#9EE3DC"),
        new("Green", "#A8DAB5"),
        new("Purple", "#D4B8F0"),
        new("Orange", "#FFD3A3"),
        new("Rose", "#F7B9D2"),
        new("Graphite", "#C9CCD1"),
    ];
}
