namespace ClipShelf.Core.Input;

// Formats and reads hotkeys the way Windows shows them: "Win+Ctrl+Alt+Shift+V".
public static class HotkeyText
{
    public static string Format(HotkeyGesture gesture)
    {
        var parts = new List<string>(5);
        if (gesture.Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        if (gesture.Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (gesture.Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (gesture.Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        parts.Add(KeyName(gesture.VirtualKey));
        return string.Join('+', parts);
    }

    public static bool TryParse(string? text, out HotkeyGesture? gesture)
    {
        gesture = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var modifiers = HotkeyModifiers.None;
        uint? key = null;
        foreach (var raw in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryModifier(raw, out var modifier))
            {
                if (key is not null) return false;      // a modifier after the main key is malformed
                modifiers |= modifier;
                continue;
            }

            if (key is not null || !TryKey(raw, out var parsed)) return false;
            key = parsed;
        }

        if (key is null) return false;

        var candidate = new HotkeyGesture(modifiers, key.Value);
        if (!candidate.IsValid) return false;

        gesture = candidate;
        return true;
    }

    public static bool IsSupportedKey(uint virtualKey) =>
        virtualKey is >= 0x30 and <= 0x39 or        // 0-9
                    >= 0x41 and <= 0x5A or          // A-Z
                    >= 0x70 and <= 0x87;            // F1-F24

    private static string KeyName(uint virtualKey) => virtualKey switch
    {
        >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(),
        >= 0x70 and <= 0x87 => $"F{virtualKey - 0x6F}",
        _ => $"0x{virtualKey:X2}",
    };

    private static bool TryModifier(string text, out HotkeyModifiers modifier)
    {
        modifier = text.ToLowerInvariant() switch
        {
            "win" => HotkeyModifiers.Win,
            "ctrl" or "control" => HotkeyModifiers.Control,
            "alt" => HotkeyModifiers.Alt,
            "shift" => HotkeyModifiers.Shift,
            _ => HotkeyModifiers.None,
        };

        return modifier != HotkeyModifiers.None;
    }

    private static bool TryKey(string text, out uint virtualKey)
    {
        virtualKey = 0;
        if (text.Length == 1)
        {
            var character = char.ToUpperInvariant(text[0]);
            if (character is >= '0' and <= '9' or >= 'A' and <= 'Z')
            {
                virtualKey = character;
                return true;
            }

            return false;
        }

        if (text.Length is >= 2 and <= 3 &&
            (text[0] is 'f' or 'F') &&
            int.TryParse(text.AsSpan(1), out var number) &&
            number is >= 1 and <= 24)
        {
            virtualKey = (uint)(0x6F + number);
            return true;
        }

        return false;
    }
}
