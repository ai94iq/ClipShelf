# DESIGN.md

Visual rules for `src/ClipShelf.Desktop` (Avalonia 12). The structure and behavior of every
surface follow its WinUI file on `main`; see the
[restart design](docs/superpowers/specs/2026-10-12-avalonia-restart-design.md) for the batch order.

## Tokens

- `<FluentTheme />` is the only theme and the only token source. Views reference Fluent keys
  through `{DynamicResource}` so light, dark and the accent follow the app without a restart.
- The accent is Fluent's `SystemAccentColor` family, set from Settings by `ThemeService`.
- Literal colors live only in `Themes/Exceptions.axaml`, per theme variant, each with a comment
  naming the Fluent gap it fills:

| Key | Job | Why it is an exception |
|---|---|---|
| `ClipShelfAccentSubtleBrush` | selected rail item, selected or checked row | Fluent has no translucent accent tint |
| `ClipShelfCardShadow` | settings groups, history list, dialogs | Fluent has no card elevation shadow |

- A color that looks wrong is fixed by choosing a different Fluent key or by an exceptions entry,
  never by a literal in a view. `App.axaml` carries no ad-hoc style setters.

## Guards

`tests/ClipShelf.Desktop.Tests/TokenRuleTests.cs` fails when a view references a key that does
not resolve in Light or Dark, when an exception stops differing between variants, or when a
literal color appears outside `Exceptions.axaml`.

## Not decided yet

Full black surfaces (renders as Dark until the dark pass, batch 8) and the dark tint value, which
is a first pick.
