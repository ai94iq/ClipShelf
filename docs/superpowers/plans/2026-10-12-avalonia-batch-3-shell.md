# Avalonia restart - Batch 3 (shell window) Implementation Plan

> Execution: inline, superpowers:test-driven-development per task. Same global constraints as
> `2026-10-12-avalonia-batches-1-2.md` (UI tests run through `Ui.Run`).

**Goal:** the tray's History and Settings items, `--history`, open a real main window: 48px title
row with the app name and (on Settings) a centered search box, a compact icon rail with History
on top and Settings in the footer, a content area that shows the selected section.

**Reference:** `src/ClipShelf.App/Shell/MainWindow.xaml` + `.xaml.cs` on `main`.

**Scope note:** the History and Settings pages arrive in batches 5 and 6; until then the content
area shows the section title. ponytail: placeholder content, replaced by the real pages.

## Review focus

1. Closing the window must hide it, not exit the app (tray-first); Exit must still close it.
2. Opening History or Settings when the window is hidden, minimized or behind other windows must
   bring it to the front.
3. Icon-only rail buttons need screen-reader names (accessibility requirement in PRODUCT.md).
4. Arabic: the window lays out right to left.
5. The search box shows only on Settings and is cleared when leaving it.

### Task 8: icons and the Tr markup extension
- Copy `src/ClipShelf.App/Resources/Icons.g.cs` to `src/ClipShelf.Desktop/Resources/Icons.g.cs`
  (namespace `ClipShelf.Desktop.Resources`); add `Controls/AppIcon.cs` (`PathIcon` with a
  `Kind` property) and `Localization/TrExtension.cs` (`{l:Tr Key=...}`).
- Tests: every `IconKind` parses to a geometry; `AppIcon` sets `Data` from `Kind`;
  `TrExtension.ProvideValue` returns the resx text for a known key.

### Task 9: MainWindow
- `Shell/MainWindow.axaml(.cs)`: as in the goal; `ExtendClientAreaToDecorationsHint` so the
  title row sits in the caption area; rail buttons are `RadioButton`s with tooltips and
  `AutomationProperties.Name`; selected style uses `ClipShelfAccentSubtleBrush`;
  `FlowDirection` from `Culture.IsRtl`; closing hides unless `AppLifetime.IsExiting`.
- Public surface: `ShowHistory()`, `ShowSettings()`, `IsSettings` (bool), `SearchText`.
- Tests (headless): ShowHistory selects History and hides search; ShowSettings selects
  Settings and shows search; leaving Settings clears search; closing hides when not exiting and
  closes when exiting; both rail buttons have automation names; RTL flips FlowDirection.

### Task 10: wire the shell
- `AppShellService` takes `Lazy<MainWindow>`; `ShowHistory/ShowSettings` call the window;
  `Exit` sets `Lifetime.IsExiting` before raising `ExitRequested`. Register `MainWindow` and
  `Lazy<>` in `AddUiServices`.
- Tests: tray History opens the window on History; Exit sets IsExiting first.
- Live check: tray History and Settings open the window; close hides it; `--history` opens it;
  Exit ends the process.
