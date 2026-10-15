# DESIGN.md

Visual rules for `src/ClipShelf.Desktop` (Avalonia 12). The structure and behavior of every
surface follow the WinUI app it replaced (in git history before the swap, ADR 0006).

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
| `ClipShelfLayerBrush` | the content layer under the title bar | main's LayerFillColorDefault: Light #EFEFEF, Dark a 5% white wash (reads over Mica), Black #000000 |
| `ClipShelfCaptionCloseBrush` | close button hover | Fluent has no caption buttons, so no close red (#C42B1C) |
| `ClipShelfCardBrush` | settings groups, history list, flyout | the card layer over the canvas: Light #FFFFFF, Dark #2B2B2B, Black #000000 |

- Avalonia's FluentTheme carries Fluent v1 control fills (buttons 20% black, inputs 40% white
  with a 60% black border). `Exceptions.axaml` maps the rest, hover and press states of buttons,
  combo boxes and text boxes onto WinUI 3's ControlFillColor and ControlStrokeColor values, so
  controls match main; a disabled text box takes ControlFillColorDisabled (Fluent's disabled
  gray read as a dark block), and focus stays Fluent's.
- Full black is its own variant (`ThemeService.Black`) inheriting Dark; only the entries above
  differ, plus a visible `SystemControlTransientBorderBrush` (#3A3A3A) so black cards keep
  their edge on the black canvas. Its dictionary is keyed with `x:Static`, since the variant type converter rejects
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

The main window runs its content into the title bar, as main does: `WindowDecorations="BorderOnly"`
with `ExtendClientAreaToDecorationsHint`, and our own title row carrying the app name, the Settings
search and the caption buttons. Avalonia 12 dropped the system-chrome hint and its own drawn
caption doubled the title and added a fullscreen button, so the row is ours and each part carries
`WindowDecorationProperties.ElementRole` (TitleBar, MinimizeButton, MaximizeButton, CloseButton;
the search is `User`). Probed live: Windows answers HTCAPTION, HTMINBUTTON, HTMAXBUTTON (snap
layouts) and HTCLOSE over those parts. The section sits on `ClipShelfLayerBrush` with a rounded
leading corner. The welcome window keeps the OS title bar.

On macOS the window keeps the system decorations (`WindowDecorations.Full`) so the traffic lights
are the system's own; the Windows-style caption buttons and the app name and icon are hidden, since
a Mac shows the app's name in the menu bar. The Settings search spans the title row and centers on
the window either way.
