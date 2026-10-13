# DESIGN.md

Visual rules for `src/ClipShelf.Desktop` (Avalonia 12). The structure and behavior of every
surface follow its WinUI file on `main`; see the
[restart design](docs/superpowers/specs/2026-10-12-avalonia-restart-design.md) for the batch order.

## Tokens

- `<FluentTheme />` is the only theme and the only token source. Views reference Fluent keys
  through `{DynamicResource}` so light, dark and the accent follow the app without a restart.
- The accent is Fluent's `SystemAccentColor` family, set from Settings by `ThemeService`.
- ClipShelf-owned resources live only in `Themes/Exceptions.axaml`, per theme variant, each with
  a comment naming the Fluent gap it fills:

| Key | Job | Why it is an exception |
|---|---|---|
| `ClipShelfAccentSubtleBrush` | selected rail item, selected or checked row | Fluent has no translucent accent tint; it is `SystemAccentColor` at 15% (light) / 25% (dark), so it follows the chosen accent |
| `ClipShelfCardShadow` | settings groups, history list, dialogs | Fluent has no card elevation shadow |
| `ClipShelfCanvasBrush` | window surface (main window, welcome) | Fluent's window and cards share one color; main's layers: Light #E6E6E6, Dark #202020, Black #000000 |
| `ClipShelfCardBrush` | settings groups, history list, flyout | the card layer over the canvas: Light #FFFFFF, Dark #2B2B2B, Black #000000 |

- Full black is its own variant (`ThemeService.Black`) inheriting Dark; only the entries above
  differ. Its dictionary is keyed with `x:Static`, since the variant type converter rejects
  custom variants.
- `ThemeService` overrides two Fluent behaviors in code, because they depend on the chosen
  accent: text and glyphs on an accent fill (accent buttons, checked toggles and boxes) take
  black on a light accent and white on a dark one, and accent-colored text on Light uses the
  accent's darker shade (`SystemAccentColorDark2`).
- Backdrops (`Services/WindowSurface.cs`): Light and full black are opaque, Dark and System
  show the chosen backdrop, the flyout is always acrylic; a window the backdrop does not reach
  keeps its solid surface.

- A color that looks wrong is fixed by choosing a different Fluent key or by an exceptions entry,
  never by a literal in a view. `App.axaml` carries no ad-hoc style setters.

## Guards

`tests/ClipShelf.Desktop.Tests/TokenRuleTests.cs` fails when a view references a key that does
not resolve in Light, Dark or Black, when an exception stops differing between variants, or when a
literal color appears outside `Exceptions.axaml`.

## Window chrome

The OS draws the title bar on every platform. Avalonia 12's own drawn caption (with
`ExtendClientAreaToDecorationsHint`) doubled the title and added a fullscreen button, so the
title row from the WinUI app is not reproduced; the Settings search sits at the top of the
window instead.
