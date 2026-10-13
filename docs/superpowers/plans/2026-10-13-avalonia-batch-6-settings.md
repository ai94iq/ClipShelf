# Avalonia restart - Batch 6 (Settings) Implementation Plan

> Execution: inline, superpowers:test-driven-development on every task (RED, GREEN, one
> mutation check). Steps use `- [ ]` checkboxes.

**Goal:** the Settings section of the main window on Avalonia, bound to main's
`SettingsViewModel` unchanged: the groups, the search filter, the restart prompt, the hotkey
recorder with its warning (P1-4), export and the update check.

**Spec:** `docs/superpowers/specs/2026-10-12-avalonia-restart-design.md`, row 6.
**Reference view:** `src/ClipShelf.App/Features/Settings/SettingsPage.xaml(.cs)` on main.

## Scope cuts (each marked `ponytail:` in code)

- Rows that need a dialog wait for batch 7, hidden, not dead: set/change/remove the lock
  password, lock a category when no password exists yet, unlock, rename, delete. Export with
  a password set needs the verify prompt, so Export is disabled while a password is set.
- Backdrop (Mica/Acrylic) is applied in batch 8 with the surfaces; the row is hidden until then.
- Tray double-click: Avalonia's tray has no double-click event (batch 3 note); the row is hidden
  until it is wired.

## Task 1: page, cards, groups and the search

**Files:** create `Features/SettingsPage.axaml(.cs)`, `Controls/SettingsCard.cs`; modify
`Shell/MainWindow.axaml(.cs)` (hosts `Lazy<SettingsPage>?`, search box text goes to
`ViewModel.Filter`), `Hosting/ServiceRegistration.cs`; tests `SettingsPageTests.cs`,
`SettingsKit` in `SettingsTestKit.cs`, `MainWindowTests.cs`.

- [ ] RED: a page over a real `SettingsViewModel` (faked services) shows every group header;
  filter "tray" hides Appearance and keeps Tray; "zzzz" shows the no-matches line; the main
  window hosts the page on Settings and its search box drives `Filter`; toggling "Clear on sign
  out" saves; changing the theme calls `IThemeService.Apply`; the max-items box saves a number.
- [ ] GREEN: `SettingsCard : HeaderedContentControl` with `Description`, template in the
  page's styles (header and description left, control right, wraps under 560 px is not needed:
  the window minimum is 560). Groups are bordered cards with dividers, Fluent keys only.
- [ ] Commit `desktop: feat: settings page with groups and search`.

## Task 2: accent swatches and the restart prompt

**Files:** `Controls/AccentSwatchConverter.cs`, page, `Shell/AppShellService.cs`
(`RestartRequested`, `Restart()`), `App.axaml.cs` (sets `App.RestartOnExit`, exits),
`Program.cs` (after the run: release the single instance, start the new process).

- [ ] RED: picking a swatch sets `SelectedAccent`; the restart bar shows only when
  `NeedsRestart`; Restart now raises `RestartRequested`; the converter gives the preset color
  and a readable check color, and the platform accent for "Windows accent".
- [ ] GREEN, commit `desktop: feat: accent swatches and the restart prompt`.

## Task 3: hotkey recorder and the conflict warning (P1-4)

**Files:** `Platform/HotkeyCapture.cs` (portable: Avalonia `Key` + `KeyModifiers` to a
`HotkeyGesture`), page.

- [ ] RED: Ctrl+Shift+V and Win+Alt+F5 capture; a modifier alone or no modifier gives null;
  click starts recording and shows the prompt; Esc cancels; a valid chord saves; the warning
  bar shows with `HasHotkeyWarning`, and an alternative button applies that gesture.
- [ ] GREEN, commit `desktop: feat: hotkey recorder with the conflict warning`.

## Task 4: export, updates, categories, exit

**Files:** page.

- [ ] RED: Export is disabled while a password is set; the export status and error lines
  follow the view model; Check for updates calls the checker; the download link shows only
  with an update; category rows list names and Lock now locks; Exit calls the shell.
- [ ] GREEN: export picks a path with `StorageProvider.SaveFilePickerAsync` (untested seam,
  verified live), commit `desktop: feat: export, updates and categories in settings`.

## Gate

Full `test.bat --no-pause` green, 0 warnings; live screenshots of Settings (light), search
filtering, the hotkey recorder; owner review.
