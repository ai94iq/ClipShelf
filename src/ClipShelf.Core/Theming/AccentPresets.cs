namespace ClipShelf.Core.Theming;

// Curated accents: each has at least 4.5:1 contrast with white text (WCAG AA).
// The display name comes from resx key "Accent_{Key}". Null accent means "use the Windows accent".
public static class AccentPresets
{
    public static IReadOnlyList<AccentPreset> All { get; } =
    [
        new("Blue", "#0F6CBD"),
        new("Teal", "#03787C"),
        new("Green", "#107C10"),
        new("Purple", "#5C2E91"),
        new("Orange", "#B7470F"),
        new("Rose", "#B4235A"),
        new("Graphite", "#4F5B66"),
    ];
}
