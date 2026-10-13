# ClipShelf - Avalonia restart design

Restarts Phase 2 of the [cross-platform migration design](2026-10-08-cross-platform-migration-design.md)
on branch `avalonia`, cut from `main`. The first attempt (branch `cross-platform`) went through
three visual directions and patched views that each picked their own tokens; it stays on origin
as a read-only reference. No code is carried from it.

## What stays from the migration design

Sections 4 (architecture, `ClipShelf.Presentation`, platform interfaces, single instance, data
compatibility), 5 (capability matrix and fallbacks), 8 (verification) and 9 (packaging) apply
unchanged. `main` already holds Phase 1: Presentation, the platform interfaces with Windows
implementations, and the named-pipe single instance with `--toggle` and `--history`.

## What changes

1. **Order.** Windows parity and the swap come first. Linux and macOS (phases 3 and 4) start only
   after `main` ships the Avalonia app.
2. **Toolkit floor.** Avalonia 12.1.x, the latest stable at scaffold time. 12.x adds the Linux
   AT-SPI accessibility tree, tray fixes and an opt-in Wayland backend.
3. **One token layer.** `<FluentTheme />` is the only theme. Views reference Fluent keys
   directly through `{DynamicResource}`; there is no alias layer, because a
   `StaticResource` alias resolves once at load and can stay on the startup variant after a
   theme switch. Literal colors are allowed only in `Themes/Exceptions.axaml`, per variant,
   each with a comment naming the missing Fluent key. Initial exceptions: the accent subtle
   tint (selected rail item, selected row) and the card shadow. `App.axaml` carries no ad-hoc
   setters. `DESIGN.md` holds the current list.
4. **Visual reference.** Each surface matches its WinUI file on `main` in structure and
   behavior. `DESIGN.md` is rewritten for this token rule as the shell lands.
5. **Process.** One surface per batch. Each batch: build, tests, run the app, owner looks, then
   commit or revert. Headless UI tests land with their surface.

## Build order

| # | Batch | Reference on `main` | Must keep |
|---|---|---|---|
| 1 | Scaffold `ClipShelf.Desktop` and `ClipShelf.Desktop.Tests`, token layer | - | - |
| 2 | Host: DI, single instance, tray, Windows platform wiring, image codec | `src/ClipShelf.App/App.xaml.cs`, `Platform/` | - |
| 3 | Shell: 48px title row, compact rail, Settings in footer, settings search box | `Shell/MainWindow.xaml` | - |
| 4 | Flyout: search, list, pick and paste-back, undo, inline clear confirm, keys | `Features/ClipboardPanel/ClipboardPanelWindow.xaml` | - |
| 5 | History: search, category filter, Pinned and Recent, select mode | `Features/History/HistoryPage.xaml` | P1-1, P1-2, P1-3 |
| 6 | Settings: eight groups, restart prompt, inline export and update status | `Features/Settings/SettingsPage.xaml` | P1-4 |
| 7 | Dialogs (code-built ContentDialogs) and welcome window | `Shell/WelcomeWindow.xaml`, dialog code in `Features/` | - |
| 8 | Dark, full black and Arabic RTL pass | - | - |
| 9 | Parity walk and swap | README feature list | - |

The P1 items are from the 2026-10-06 design critique (kept locally, not committed). `main`
already fixes all four in the shared view models; the Avalonia views must wire them, not
re-solve them: P1-1 a true total and load-more (`TotalCount`, `LoadMoreCommand`); P1-2 the
transient "Copied" footer in History; P1-3 keyboard-operable rows and Esc cancelling select mode
or the clear confirm; P1-4 a failed rebind keeps the previous shortcut and warns on the hotkey
row (`HasHotkeyWarning`).

## Acceptance for the swap

- Every README feature works in the Avalonia app on Windows; all four P1 behaviors kept.
- `test.bat` green, including the headless UI tests.
- Manual Windows check: tray, hotkey, text and image capture, paste-back, locks, export.
- Light, dark, full black and Arabic RTL reviewed by the owner on every surface.
- Then per the migration design, Phase 2: merge to `main`, remove `ClipShelf.App`, Windows App
  SDK and `H.NotifyIcon`, update the MSI harvest.

## Execution

Inline, one session per batch, no agent swarm: each batch is gated on the owner reviewing the
running app, so parallel agents add review cost without speed. Revisit for the Linux platform
layer after the swap.
