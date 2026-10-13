# Avalonia restart - Batch 7 (dialogs and welcome) Implementation Plan

> Execution: inline, superpowers:test-driven-development on every task (RED, GREEN, one
> mutation check). Steps use `- [ ]` checkboxes.

**Goal:** every dialog main shows, on Avalonia, and the first-run welcome window; this also
restores the rows batch 6 cut for lack of dialogs.

**Reference:** main's `Features/Settings/PasswordPrompt.cs`, `SettingsPage.xaml.cs`,
`Features/ClipboardPanel/CategoryMenu.cs`, `Features/History/HistoryPage.xaml.cs`,
`Shell/WelcomeWindow.xaml(.cs)`.

**Decision:** Avalonia has no ContentDialog. A separate modal window would take the focus, and
the flyout hides when it loses focus, so a dialog is drawn inside its window instead: a scrim and
a card on the window's overlay layer (`InlineDialog`). Window key handlers (flyout, History)
stand aside while one is open, so Enter and Esc belong to the dialog.

## Task 1: InlineDialog and the password prompts

**Files:** `Controls/InlineDialog.axaml(.cs)`, `Features/PasswordPrompt.cs`; tests
`InlineDialogTests.cs`, `PasswordPromptTests.cs`, `DialogDriver` helper.

- [ ] RED: a dialog shows over the window with its title and content and focuses the first
  input; Cancel and Esc give false; the primary button and Enter give true; an accept check
  that fails keeps it open; a disabled primary ignores Enter. Prompts: Verify retries on a
  wrong password and shows the error; Set rejects short and mismatched passwords; Change needs
  the current one; Remove needs it too.
- [ ] GREEN, commit `desktop: feat: in-window dialogs and the password prompts`.

## Task 2: Settings rows that need a dialog

**Files:** `Features/SettingsPage.axaml(.cs)`.

- [ ] RED: password row Set/Change/Remove; Lock with no password asks to set one first; Unlock
  asks for the password; Rename keeps the dialog open on a taken name; Delete confirms; Export
  with a password asks for it (Export no longer disabled).
- [ ] GREEN, commit `desktop: feat: settings password, lock and category dialogs`.

## Task 3: History filter unlock and the row category menu

**Files:** `Features/HistoryPage.axaml.cs`, `Controls/ClipRow.axaml`, `Controls/CategoryMenu.cs`,
`Shell/FlyoutWindow.axaml.cs`.

- [ ] RED: picking a locked category in the filter asks for the password, keeps it on success
  and falls back on cancel; the row category button opens a menu with None, each category
  (current one checked) and New category; picking one assigns it; New category creates and
  assigns; the flyout key handler ignores keys while a dialog is open.
- [ ] GREEN, commit `desktop: feat: category menu and locked filter unlock`.

## Task 4: welcome window

**Files:** `Shell/WelcomeWindow.axaml(.cs)`, `App.axaml.cs`, `Hosting/ServiceRegistration.cs`.

- [ ] RED: the window names the saved hotkey; OK closes it; the first run shows it once and
  saves `WelcomeShown`.
- [ ] GREEN, commit `desktop: feat: first-run welcome window`.

## Gate

`test.bat --no-pause` green, 0 warnings; live screenshots of a password prompt, the category
menu and the welcome window. Owner reviews batches 6-9 together at the end (owner, 2026-10-13).
