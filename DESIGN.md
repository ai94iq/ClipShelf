# DESIGN.md

<!-- impeccable:design-schema 1 -->
<!-- World: Memry card language (surface brief .impeccable/surfaces/src-clipshelf-desktop.md, amendment 2026-10-10). Scope: src/ClipShelf.Desktop on Avalonia 11.3.22. Light and Dark token sets; BoxShadow literals stay light-tuned. -->

## Ground and surfaces

- Light: muted system gray `#F5F5F7` ground, white `#FFFFFF` cards. Dark: `#1C1C1E` ground, `#2C2C2E` cards, `#232327` rail. Both sets live in `Themes/Colors.axaml`, light at base level, dark in the theme dictionary; text and borders flip with them, accent stays `#3B82F6`.
- Floating cards: corner radius 10, 1px subtle border, soft shadow `0 4 16 -4 #1A1A2E` (literal, tuned for light). Used for settings groups, the history list, dialogs (`0 8 24 -8` on dialogs).

## Accent (one job each)

- Accent blue `#3B82F6` (`AccentBrush`; hover `#2563EB`, press `#1D4ED8`).
- Selected sidebar row: blue pill, `AccentSubtleBrush` (`#DBEAFE`) ground with accent icon and label.
- Selected/checked clip row: same `AccentSubtleBrush` wash.
- Success `#059669`, error `#DC2626` with `#FEE2E2` subtle ground.

## Type

- Headings 24/20 semibold, card headers 15 regular, body 14/13, meta 12/11 in `TextSecondary`/`TextMuted`. Clip meta uses tabular figures. No display face, no kickers.

## Components

- `SettingsCard`: one row, header plus optional description left, control right, min height 64, hairline dividers between rows.
- Clip cell: snippet (2-line ellipsis) or thumbnail, app name plus relative time below, pin/category/delete icon buttons on the right with hover wash. Image thumbs carry a dark media badge with the image glyph, bottom left.
- Undo bar: full-width card, "deleted" text plus Undo, shown while undo is live.
- LoadStateOverlay: indeterminate bar (loading), centered icon plus title plus hint (empty), error text plus Retry (error).
- Dialogs (welcome, password, rename, confirm, new category): centered card, 20pt semibold title, 120x40 paired buttons, Cancel right of primary. No message-body copy beyond titles; WinUI parity.

## Behavior kept from WinUI

- Flyout: search, single list, icon-only footer, deferred-delete undo, inline clear confirm. Left click picks, right click opens the category menu, Enter picks, arrows move selection, Esc steps back.
- History: Pinned then Recent sections with headers, search plus category filter, footer with count/notice and select mode, same undo and confirm flows.
- Settings: eight groups (appearance, tray, behavior, history, export, categories/locks, updates, exit) plus restart prompt, hotkey conflict alternatives, inline export/update status.

## Retired

- Visible construction-grid hairlines and the saturated blue rail from the earlier direction. No glass, no gradients, no kickers, no nested cards.
