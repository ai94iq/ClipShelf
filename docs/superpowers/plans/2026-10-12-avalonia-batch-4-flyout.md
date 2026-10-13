# Avalonia restart - Batch 4 (flyout) Implementation Plan

> Execution: inline, superpowers:test-driven-development per task; UI tests through `Ui.Run`.

**Goal:** the hotkey or a tray click opens a borderless 360x480 flyout next to the pointer;
search, a list of clips, pick with click or Enter (or Ctrl+1..9) pastes into the app the user
came from; pin, delete with undo, clear with an inline confirm, select mode; Esc steps back.

**Reference:** `src/ClipShelf.App/Features/ClipboardPanel/*` and `Platform/PanelFrame.cs` on
`main`. The view model (`ClipboardPanelViewModel`) is shared and unchanged.

**Deferred:** the row category menu (needs the category and password dialogs, batch 7); the
category button is hidden until then. Acrylic: the flyout is opaque Fluent until the dark pass.

## Review focus

1. Clicking the tray icon while the flyout is open must close it, not close-and-reopen
   (the deactivation hides it a moment before the tray click toggles).
2. The pick must paste into the window that was in front before the flyout opened.
3. Near a screen edge or on the second monitor the flyout stays inside that monitor's work area.
4. Esc order: leave select mode, then cancel the clear confirm, only then close.
5. A clip list with images: thumbnails decode; a broken thumbnail must not break the row.

### Task 11: flyout window and platform seam
- `Platform/IFlyoutPlatform` (foreground window handle, cursor position, force/restore
  foreground) with `Platform/Windows/WindowsFlyoutPlatform` from main's `PanelFrame` calls.
- `Shell/FlyoutWindow.axaml(.cs)`: no decorations, topmost, not in the taskbar; `Toggle()`,
  `ShowFlyout()`, `HideFlyout()`; placement through `PanelPlacement.NearCursor` on the screen
  under the pointer (pixels, scaled); deactivation hides; a toggle within 300 ms of a
  deactivation hide stays closed (`TimeProvider`).
- Tests: toggle opens then closes; showing resets transient state and marks the view visible;
  placement stays in the work area; toggle right after a deactivation hide keeps it closed.

### Task 12: list, rows, keys and pick
- Search box bound to `SearchText`; `ListBox` over `Items` with `SelectedItem`; row template
  (thumbnail or preview, source line, pin and delete buttons); `BytesToBitmapConverter`;
  keys: Up/Down, Enter, Ctrl+1..9, Esc (`DismissTransient` first); click picks;
  `ItemActivated` hides and pastes into the remembered window; load more at the last row.
- Tests: converter (bytes, null, garbage); Down then Enter writes that clip and pastes into the
  previous window; Ctrl+2 picks the second; Esc while selecting only leaves select mode.

### Task 13: footer, undo, clear confirm, load states
- Footer: open history, select, cancel, copy selection, clear all (tooltips, automation names);
  undo bar while a delete is pending; inline clear confirm over the content; empty, loading and
  error states with retry.
- Tests: footer buttons have automation names; the confirm shows while confirming; the empty
  state shows `EmptyTitle`.

### Task 14: wire it
- `AppShellService.TogglePanel` drives the flyout; `OpenHistoryRequested` opens the main
  window; register flyout, view model, platform; build the flyout at startup so the first
  open is instant.
- Live: hotkey opens near the pointer, typing filters, Enter pastes into Notepad, tray click
  toggles, Esc closes; screenshots light.
