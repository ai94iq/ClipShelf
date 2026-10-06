namespace ClipShelf.Core.Theming;

// Curated accents from the Windows 11 color grid, so the picker offers the same colors as
// Windows itself. The display name comes from resx key "Accent_{Key}". Null accent means
// "use the Windows accent".
public static class AccentPresets
{
    public static IReadOnlyList<AccentPreset> All { get; } =
    [
        new("Blue", "#0078D4"),
        new("Teal", "#00B7C3"),
        new("Green", "#10893E"),
        new("Purple", "#B146C2"),
        new("Orange", "#FF8C00"),
        new("Rose", "#E3008C"),
        new("Graphite", "#767676"),
    ];
}
