namespace ClipShelf.Core.Theming;

// Curated pastel accents: soft but saturated enough to read on dark surfaces. The display name
// comes from resx key "Accent_{Key}". Null accent means "use the Windows accent".
public static class AccentPresets
{
    public static IReadOnlyList<AccentPreset> All { get; } =
    [
        new("Blue", "#8AB4F8"),
        new("Teal", "#6DD3C4"),
        new("Green", "#81C995"),
        new("Purple", "#C58AF9"),
        new("Orange", "#FFB077"),
        new("Rose", "#F48FB1"),
        new("Graphite", "#9FA6B0"),
    ];
}
