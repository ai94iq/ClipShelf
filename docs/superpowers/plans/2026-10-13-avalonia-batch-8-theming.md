# Avalonia restart - Batch 8 (dark, full black, RTL) Implementation Plan

> Execution: inline, superpowers:test-driven-development on every task (RED, GREEN, one
> mutation check). Steps use `- [ ]` checkboxes.

**Goal:** the theme settings look right in every combination main supports: Light, Dark, full
Black and System; each accent preset; the backdrop; and Arabic right-to-left.

**Reference:** main's `Services/ThemeService.cs`, `Common/ThemeSurfaces.cs`,
`Common/WindowSurface.cs`.

## What main does (kept)

- Light: opaque, a soft gray canvas (#E6E6E6) with white cards.
- Full black: opaque black canvas and cards; borders keep the layers apart.
- Dark and System: the chosen backdrop (Mica, Mica Alt, Acrylic, None).
- The accent is set once at startup (restart prompt stays, batch 6).

## Task 1: accent contrast and the Black variant

**Files:** `Services/ThemeService.cs`, `Themes/Exceptions.axaml`, tests `ThemeServiceTests.cs`,
`TokenRuleTests.cs`.

- [ ] RED: text and glyphs on an accent fill (accent button, checked toggle knob, checked box)
  read on the accent: black on the pastel presets, white on a dark accent, Fluent's own with no
  accent. Black maps to a `Black` variant that inherits Dark; every referenced key resolves in it.
- [ ] GREEN, commit `desktop: feat: readable text on the accent and a full black variant`.

## Task 2: surfaces

**Files:** `Themes/Exceptions.axaml` (`ClipShelfCanvasBrush`, `ClipShelfCardBrush` per Light,
Dark, Black, each naming the Fluent gap), views (windows on the canvas, cards on the card brush).

- [ ] RED: canvas and card resolve per variant with main's values; Light canvas and card differ;
  Black canvas is black; views reference the tokens (token rule keeps literals out of views).
- [ ] GREEN, commit `desktop: feat: canvas and card surfaces per theme`.

## Task 3: backdrop

**Files:** `Services/WindowSurface.cs`, windows attach; `Features/SettingsPage.axaml` (the row
returns).

- [ ] RED: Dark or System with Mica asks the window for Mica and clears its background; None, Light
  and Black stay opaque on the canvas; a theme change re-applies to every attached window.
- [ ] GREEN, commit `desktop: feat: mica and acrylic backdrops`.

## Task 4: dark, black and RTL walk

Live screenshots of the shell, flyout, History, Settings, a dialog and the welcome window in
Dark, Black and Arabic; fix what reads wrong, each fix test-first where a test can see it.

## Gate

`test.bat --no-pause` green, 0 warnings; the owner's settings file restored after the walk.
