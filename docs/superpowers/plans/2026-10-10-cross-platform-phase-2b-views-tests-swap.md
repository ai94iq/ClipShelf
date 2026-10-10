# Phase 2b - Remaining Views, Tests, Review, Swap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the history window, settings window, welcome window and dialogs to the Avalonia head with behavior parity to the current WinUI app, cover them with headless UI tests, pass the impeccable finish review, then remove the WinUI app and ship the MSI from the Desktop head.

**Architecture:** Views consume the already-ported Presentation view models unchanged; only view-layer concepts are re-expressed (grouped sections instead of CollectionViewSource, StorageProvider instead of WinRT pickers, SplitButton instead of DropDownButton, notice rows instead of InfoBar). The swap deletes `ClipShelf.App` and repoints packaging at `ClipShelf.Desktop`.

**Tech Stack:** .NET 10 (SDK 10.0.401), Avalonia 12.1.3 + Avalonia.Headless 12.1.3 (both CPM-pinned), SkiaSharp, CommunityToolkit.Mvvm, xUnit v3 + NSubstitute on Microsoft.Testing.Platform, WiX v6 (installer project only, not CPM).

**Spec:** `docs/superpowers/specs/2026-10-08-cross-platform-migration-design.md`
**Surface brief (binding direction contract):** `.impeccable/surfaces/src-clipshelf-desktop.md`
**Direction:** The Construction Grid (catalog challenger crouwel-grid-specimen; seed key d14438e0; code-led build).

## Global Constraints

- **The direction contract binds every view.** Type, palette, density, and the one signature move only; layouts stay standard controls. A view that reads as a poster instead of a working product fails the contract.
- **Behavior parity with the WinUI app on main, not with the old critique.** The critique's four P1 items (incremental history with true totals, visible copy feedback, keyboard-operable history with working Esc, hotkey failure keeping the previous shortcut plus an inline warning) are already fixed in the WinUI app; port the fixed behaviors and verify each in the acceptance steps.
- **ASCII only** in code, comments, docs and commit messages: no em/en dashes, no arrows, no decorative symbols.
- **Commit format:** `<project>: <type>: <title>`; `desktop`, `presentation`, `tests`, `docs`, `repo`, `installer` per README. All commits land on the `cross-platform` branch. Never push without explicit approval.
- **CPM versions are fixed:** Avalonia family 12.1.3. CA1416 is an error: Windows-only call sites carry `[SupportedOSPlatform("windows")]` with `OperatingSystem.IsWindows()` guards (directly enclosing the call, including inside lambdas).
- **Build hygiene:** stop `ClipShelf.exe` before builds and commits (exe lock). The pre-commit hook runs the full suite; never bypass it.
- **No system modifications without explicit approval.** Registry, Explorer, services, OS settings and other apps' data are read-only. Probe-then-assert still applies to the app itself.
- **No subagents without explicit approval.** The finish review and documentation run as in-thread passes; this is weaker than a fresh reviewer and is disclosed, not hidden.

## Review Focus

1. **Locked-category filter pick without the password** - the prompt appears; cancel reverts the selection. (Task 2: headless test `LockedFilterCancel_RevertsSelection`.)
2. **Failed export** - an unwritable export path shows the error status, no crash, no partial file left behind. (Task 3: VM test `ExportAsync_UnwritablePath_ShowsError` in Presentation.Tests.)
3. **Hotkey rebind to a taken combo** - the previous shortcut keeps working and the warning names it. (Task 3: VM test with failing `IHotkeyRegistration` fake asserting `HasHotkeyWarning` and preserved `RegisteredHotkey`.)
4. **Restart after an accent or language change** - the relaunched process applies the new accent. (Task 5: manual smoke step; restart cannot be covered in-suite.)
5. **MSI upgrade over the WinUI install** - history, settings and key survive; uninstall keeps `%LOCALAPPDATA%\ClipShelf`. (Task 8: manual install checklist.)

---

### Task 1: Shared view infrastructure

**Files:**
- Move: `src/ClipShelf.App/Resources/Icons.g.cs`, `src/ClipShelf.App/Resources/icons.txt` -> `src/ClipShelf.Desktop/Resources/` (namespace `ClipShelf.Desktop.Resources`; update the generator-script path in the header comment if `scripts/add-icons` moved with it)
- Create: `src/ClipShelf.Desktop/Controls/AppIcon.cs`, `src/ClipShelf.Desktop/Controls/SettingsCard.cs`, `src/ClipShelf.Desktop/Common/RtlAlignExtension.cs`, `src/ClipShelf.Desktop/Common/AccentSwatchConverter.cs`, `src/ClipShelf.Desktop/Common/PinIconConverter.cs`, `src/ClipShelf.Desktop/Themes/Cells.axaml`
- Move: `src/ClipShelf.App/Common/RtlAlign.cs` -> `src/ClipShelf.Desktop/Common/RtlAlign.cs` (type change `Microsoft.UI.Xaml.HorizontalAlignment` -> `Avalonia.Layout.HorizontalAlignment`, logic identical)
- Modify: `src/ClipShelf.Desktop/Themes/Controls.axaml` (settings-card + swatch styles), `src/ClipShelf.Desktop/Views/FlyoutWindow.axaml` (consume the extracted template), `src/ClipShelf.Desktop/App.axaml` (merge Cells.axaml)
- Move tests: `tests/ClipShelf.App.Tests/IconTests.cs`, `tests/ClipShelf.App.Tests/RtlAlignTests.cs`, `tests/ClipShelf.App.Tests/TrayIconFileTests.cs` -> `tests/ClipShelf.Desktop.Tests/` (namespace updates only; the icon-usage regex already matches `.axaml` since it ends with `.xaml`)

**Interfaces:**
- Consumes: 2a tokens, `TrExtension`, converters pattern.
- Produces: `AppIcon { Kind: IconKind, IsDirectional: bool }`, `SettingsCard { Header: string, Description: string }`, `RtlAlignExtension { Desired }`, `AccentSwatchConverter` (hex -> Brush, `Check` param -> adaptive check color), `PinIconConverter` (bool -> IconKind), `ClipCellTemplate` (shared `DataTemplate` for `ClipItemViewModel`: thumbnail, 2-line preview, meta line, pin mark).

- [ ] **Step 1: Move the icon data and the pure-logic helper**

`git mv` the three files; rewrite namespaces. `RtlAlign.For` keeps its signature with the Avalonia alignment type.

- [ ] **Step 2: Write the controls and converters**

`AppIcon`: a `Control` with `KindProperty` and `IsDirectionalProperty`; renders a `Path` whose `Data` is `StreamGeometry.Parse(IconData.Get(Kind))` filled with `Foreground`; non-directional kinds force left-to-right flow. `SettingsCard`: a `ContentControl` with `Header`/`Description` styled properties; default style in Controls.axaml (header label + muted description + content, grid hairline separators come from the page). Converters implement `IValueConverter` (ConvertBack throws `NotSupportedException`).

- [ ] **Step 3: Extract the shared cell template**

Move the flyout's inline `DataTemplate` for `ClipItemViewModel` unchanged into `Cells.axaml` as `ClipCellTemplate`; merge Cells.axaml in App.axaml; FlyoutWindow references it by `StaticResource`. No visual change (verified by rebuilding and eyeballing the flyout).

- [ ] **Step 4: Move the tests**

`git mv` the three test files; update namespaces/usings. `TrayIconFileTests` now resolves `TrayIconFiles` from the Desktop head.

- [ ] **Step 5: Build and run the full suite**

Run: `dotnet test --solution ClipShelf.slnx -c Release`
Expected: all green, count grows by 0 (moved tests keep running).

- [ ] **Step 6: Commit**

```bash
git add src/ClipShelf.Desktop tests/ClipShelf.Desktop.Tests
git commit -m "desktop: feat: shared view infrastructure for the remaining windows"
```

---

### Task 2: History window

**Files:**
- Create: `src/ClipShelf.Desktop/Views/HistoryPage.axaml` + `.axaml.cs`
- Modify: `src/ClipShelf.Desktop/Hosting/DesktopServiceRegistration.cs` (register `HistoryPage` singleton)

**Interfaces:**
- Consumes: `ClipboardPanelViewModel` (shared instance with the flyout), `ClipCellTemplate`, `StateEqualsConverter`, `CategoryMenu.ShowAsync`, `PasswordDialogs.Verify` (Task 4).
- Produces: `HistoryPage { IsActive: bool, LoadAsync(): Task }` showing grouped sections; wired into MainWindow in Task 5.

- [ ] **Step 1: Build the page structure**

Header (title + description), search + category-filter row (`ComboBox` with `ItemTemplate` and `SelectionBoxItemTemplate` showing `Label`, `SelectedItem` TwoWay to `SelectedCategoryFilter`), scrollable `ViewModel.Groups` sections (per `ClipGroup`: muted header from `Title` + rows `ItemsControl` with each row wrapped in a borderless full-width `Button` with `Classes="clip-row"`, `Command` bound to the row's `ActivateCommand`, content from the shared `ClipCellTemplate`), load-state overlay reusing the flyout's `StateEquals` pattern, undo bar, footer (count + green copy notice + select/copy/cancel/clear buttons), clear-confirmation overlay with dimmed backdrop.

- [ ] **Step 2: Wire the behaviors**

`LoadAsync()` calls `RefreshAsync` + `RefreshCategoryFilterAsync`. Click = `Activate` + clear selection wash. Tunneling `KeyDown`: Up/Down call `MoveRowFocus(delta)`, which collects row buttons (`Classes="clip-row"` on each row's Button, in visual order via `GetVisualDescendants().OfType<Button>()`) and focuses the next/previous one; Enter activates natively through the focused button; Esc calls `DismissTransient` (page stays open). `ScrollViewer.ScrollChanged`: when the offset is within 200 px of the bottom and the view model is `IIncrementalSource` with `HasMore`, execute `LoadMoreCommand`. Category-filter change on a locked category opens the password dialog and reverts on cancel. Row category menu reuses `CategoryMenu.ShowAsync` at the pointer. `IsActive` gates menu handling (shared VM with the flyout).

- [ ] **Step 3: Smoke the P1 items**

Run the app, open History: scroll loads older clips and the footer shows the true total; click a row shows the green Copied notice; arrows + Enter work from the keyboard; Esc cancels the clear confirmation.

- [ ] **Step 4: Commit**

```bash
git add src/ClipShelf.Desktop
git commit -m "desktop: feat: history window with grouped sections"
```

---

### Task 3: Settings window

**Files:**
- Create: `src/ClipShelf.Desktop/Views/SettingsPage.axaml` + `.axaml.cs`, `src/ClipShelf.Desktop/Platform/Windows/DesktopHotkey.cs`
- Create: `tests/ClipShelf.Presentation.Tests/FailedExportTests.cs` (Review Focus 2), extend hotkey VM coverage for Review Focus 3
- Modify: `src/ClipShelf.Desktop/Shell/TrayController.cs` (add `Restart()`), `src/ClipShelf.Desktop/Hosting/DesktopServiceRegistration.cs` (register `SettingsPage` singleton)

**Interfaces:**
- Consumes: `SettingsViewModel` (Presentation, unchanged), `PasswordDialogs` (Task 4), `TrayController.Exit/Restart/ShowHistory/ShowSettings`.
- Produces: `SettingsPage { LoadAsync(): Task }` with all eight groups; `DesktopHotkey.TryCapture(Key, KeyModifiers): HotkeyGesture?`.

- [ ] **Step 1: Port the page structure**

All eight groups from the WinUI page (appearance incl. accent swatches as `RadioButton`s with the ported swatch template, tray, behavior incl. hotkey recorder button, history, export, categories, updates, exit). Replacements: `ComboBox` with label templates; `ToggleSwitch.IsChecked` TwoWay; `NumericUpDown` (5..1000, increment 5) for max items; `SplitButton` with three checkable `MenuItem`s TwoWay-bound to the lock flags for LockWhen; notice rows (world-styled bordered rows) instead of InfoBar for the restart prompt and hotkey warning; `HyperlinkButton` with the hardcoded releases URL for downloads. `BoolToVisibility` needs no port: Avalonia binds `bool` directly to `IsVisible`.

- [ ] **Step 2: Port the code-behind**

Hotkey recorder (click to record, key down captures, Esc cancels, focus loss stops) via `DesktopHotkey.TryCapture`: modifiers from `KeyModifiers` (Control/Alt/Shift/Meta-as-Win), main key mapped by an explicit `ToVirtualKey(Key)` table; modifier-only presses keep waiting. Export via `TopLevel.StorageProvider.SaveFilePickerAsync` with JSON/CSV file-type choices, path from `TryGetLocalPath()`, then `ViewModel.ExportAsync`. Password flows call `PasswordDialogs`; rename/delete category call the Task 4 dialogs. Restart calls `TrayController.Restart()` (`Process.Start(Environment.ProcessPath)` then `Exit()`); Exit calls `tray.Exit()`.

- [ ] **Step 3: Pin the new logic with tests**

`DesktopHotkey` unit test in Desktop.Tests: A-Z map to `0x41-0x5A`, D0-D9 to `0x30-0x39`, F1-F12 to `0x70-0x7B`, modifier-only input returns null. Run: `dotnet test tests/ClipShelf.Desktop.Tests -c Release`. Expected: PASS (TDD: test first, watch fail, implement).
`ExportAsync_UnwritablePath_ShowsError` in Presentation.Tests: export to an unwritable path asserts `ShowExportError` and no throw.
Taken-rebind VM test in Presentation.Tests: failing `IHotkeyRegistration` fake after a successful one asserts `HasHotkeyWarning` and the preserved `RegisteredHotkey`.

- [ ] **Step 4: Smoke the settings**

Run the app: every group renders; theme/accent/language combos apply (restart prompt appears, restart works); tray toggles apply; hotkey rebind to a taken combo warns and keeps the old one; export writes a file; password set/change/remove works; category lock/unlock/rename/delete works.

- [ ] **Step 5: Commit**

```bash
git add src/ClipShelf.Desktop tests/ClipShelf.Desktop.Tests tests/ClipShelf.Presentation.Tests
git commit -m "desktop: feat: settings window with all eight groups"
```

---

### Task 4: Password and category dialogs

**Files:**
- Create: `src/ClipShelf.Desktop/Views/PasswordDialog.axaml` + `.axaml.cs`, `src/ClipShelf.Desktop/Views/RenameDialog.axaml` + `.axaml.cs`, `src/ClipShelf.Desktop/Views/ConfirmDialog.axaml` + `.axaml.cs`

**Interfaces:**
- Consumes: `Tr` strings, world styles. Nothing consumes these except Tasks 2-3 call sites (already written against this API).
- Produces: `PasswordDialogs.VerifyAsync(Window, Func<string,bool>): Task<bool>`, `SetAsync(Window): Task<string?>`, `ChangeAsync(Window, verify): Task<string?>`, `ConfirmRemoveAsync(Window, verify): Task<bool>`; `RenameDialog.ShowAsync(Window, currentName, checkTaken: Func<string, Task<bool>>): Task<string?>`; `ConfirmDialog.ShowAsync(Window, title, message, primaryLabel): Task<bool>`.

- [ ] **Step 1: Build the three dialogs**

`PasswordDialog` with four modes mirroring the WinUI flows: current/new/confirm boxes per mode, inline error text, minimum length 4, mismatch detection, wrong-password message, remove-mode consequence warning; primary disabled until valid input. `RenameDialog`: name box + taken-name error loop (stays open, shows `Category_Exists`). `ConfirmDialog`: message + destructive primary + cancel. All follow the `NewCategoryDialog` TCS pattern and close on owner shutdown.

- [ ] **Step 2: Smoke each flow**

Drive from the settings page: verify wrong password shows the error; set/change/remove round-trips; rename to a taken name stays open with the error; delete asks first.

- [ ] **Step 3: Commit**

```bash
git add src/ClipShelf.Desktop
git commit -m "desktop: feat: password and category dialogs"
```

---

### Task 5: App wiring

**Files:**
- Modify: `src/ClipShelf.Desktop/Views/MainWindow.axaml` + `.axaml.cs`, `src/ClipShelf.Desktop/App.axaml.cs`
- Create: `src/ClipShelf.Desktop/Views/WelcomeWindow.axaml` + `.axaml.cs`

**Interfaces:**
- Consumes: `HistoryPage`, `SettingsPage`, `SettingsService`, `IThemeService`.
- Produces: rail navigation with `LoadAsync` on show; first-run welcome; per-window RTL font family.

- [ ] **Step 1: Wire real navigation**

Rail buttons swap `HistoryPage`/`SettingsPage` into the content host, call `LoadAsync()`, set `HistoryPage.IsActive` for the visible page. `ShowHistory()`/`ShowSettings()` navigate then show + activate (existing TrayController calls keep working).

- [ ] **Step 2: Port the welcome window**

460x360 centered (`Screens.Primary.WorkingArea` math), `WelcomeHotkey` text formatted with the current shortcut, tray explanation, OK closes. App shows it on first run when `WelcomeShown` is false, then saves the flag (same service call as the WinUI app).

- [ ] **Step 3: Per-window culture setup**

Each window root sets `FlowDirection` from `Culture.IsRtl` and the font family to the bundled Noto (`avares://ClipShelf/Assets/Fonts/NotoSansArabic-Regular.ttf#Noto Sans Arabic`) when RTL, mirroring the WinUI font override. Window icons: welcome window gets `app.ico`.

- [ ] **Step 4: Smoke the shell**

Fresh-profile run shows welcome once; rail switches pages with data; RTL (`ar`) mirrors alignment; restart from settings relaunches with the accent applied (Review Focus 4, manual).

- [ ] **Step 5: Commit**

```bash
git add src/ClipShelf.Desktop
git commit -m "desktop: feat: shell navigation, welcome and culture setup"
```

---

### Task 6: Headless UI tests

**Files:**
- Modify: `tests/ClipShelf.Desktop.Tests/ClipShelf.Desktop.Tests.csproj` (add `Avalonia.Headless` 12.1.3)
- Create: `tests/ClipShelf.Desktop.Tests/HeadlessApp.cs`, `tests/ClipShelf.Desktop.Tests/FlyoutHeadlessTests.cs`, `tests/ClipShelf.Desktop.Tests/HistoryHeadlessTests.cs`, `tests/ClipShelf.Desktop.Tests/SettingsHeadlessTests.cs`
- Create: `tests/ClipShelf.Presentation.Tests/ClipboardPanelPagingTests.cs` (incremental growth at VM level)

**Interfaces:**
- Consumes: `Program.BuildAvaloniaApp()` (existing, side-effect free), all views and view models.
- Produces: green headless coverage for the P1 behaviors and Review Focus 1.

**Harness (exact, from the 12.1.3 binaries):** `HeadlessUnitTestSession.StartNew(typeof(Program))` held in an `IDisposable` fixture; UI work via `session.Dispatch(action, TestContext.Current.CancellationToken)`. (`Avalonia.Headless.XUnit` stays out: it targets xUnit v2 only.)

- [ ] **Step 1: Write the failing tests**

```csharp
Flyout_OpensShowingItems()           // FlyoutWindow with a VM holding 3 fake clips; assert 3 realized rows
Flyout_SearchFiltersToEmptyState()   // SearchText = "zz-no-match"; assert the empty title shows
History_RendersGroupHeaders()        // HistoryPage with Pinned + Recent groups; assert both headers exist
History_LockedFilterCancel_Reverts() // select a locked category option, cancel the password dialog; assert the selection reverted (Review Focus 1)
Settings_LoadsAllGroups()            // SettingsPage renders; assert the eight section headers exist
Settings_TakenHotkey_KeepsPrevious() // VM-level: failing hotkey fake; assert HasHotkeyWarning and preserved RegisteredHotkey (Review Focus 3)
Paging_LoadMore_GrowsItems()         // Presentation.Tests: paged fake repo; LoadMoreCommand grows Items and TotalCount stays truthful
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/ClipShelf.Desktop.Tests -c Release`
Expected: FAIL (no harness, no fixture).

- [ ] **Step 3: Implement the harness, then watch them pass**

Add the package reference, the fixture, and only the production seams the tests need (nothing else). Run the full Desktop.Tests project.
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add tests/ClipShelf.Desktop.Tests tests/ClipShelf.Presentation.Tests src/ClipShelf.Desktop/ClipShelf.Desktop.csproj
git commit -m "tests: test: headless ui coverage for the ported windows"
```

---

### Task 7: Finish review and design record

**Files:**
- Create: `DESIGN.md`, `.impeccable/design.json`
- Modify: whatever the review finds (one fix pass)

**Interfaces:**
- Consumes: the direction contract (`.impeccable/surfaces/src-clipshelf-desktop.md`), the quality-bar boards (`.impeccable/reference/quality-bar/`), every view.
- Produces: the documented shipped world.

- [ ] **Step 1: Critique pass**

Screenshot each view (flyout, history, settings, welcome, dialogs, empty/error states) via PrintWindow capture; judge against the direction contract's FIRST VIEWPORT and the four world contributions (type, palette, density, signature move). Batch all findings, fix in one pass, recapture once. (In-thread passes; no reviewer subagents per standing rule. This is weaker than a fresh reviewer and is disclosed.)

- [ ] **Step 2: Audit pass**

Keyboard-only run of every flow; contrast spot-checks (mute/placeholder/footer text); RTL layout; screen-reader names on icon-only buttons; reduced-motion path.

- [ ] **Step 3: Document the world**

Write `DESIGN.md` (tokens, type ramp, controls, states, per-theme behavior) and `.impeccable/design.json` from the shipped code, then verify both match the code.

- [ ] **Step 4: Commit**

```bash
git add DESIGN.md .impeccable/design.json src/ClipShelf.Desktop
git commit -m "docs: docs: record the shipped construction grid world"
```

---

### Task 8: Swap, package, merge request

**Files:**
- Delete: `src/ClipShelf.App/`, `tests/ClipShelf.App.Tests/`
- Modify: `ClipShelf.slnx` (drop both projects), `build.bat` (publish `src\ClipShelf.Desktop\ClipShelf.Desktop.csproj` with the same flags), `installer/Package.wxs` line 15 (icon path `..\src\ClipShelf.Desktop\Assets\app.ico`), `README.md` (project table, requirements stay Windows-only for this release), `CHANGELOG.md` (Unreleased entry for the swap)
- Move: any remaining shared test helpers (none expected; verify with a grep for `ClipShelf.App` under `src/` and `tests/`)

**Interfaces:**
- Consumes: everything above.
- Produces: `main`-ready tree with the MSI built from the Desktop head. No push without approval.

- [ ] **Step 1: Remove the WinUI app**

`git rm` the two projects; drop them from the slnx; repoint build.bat and the wxs icon path. `test.bat` needs no change (solution-driven). Version stays 0.3.3 (set at release time per the spec).

- [ ] **Step 2: Verify from clean**

Run: `test.bat --no-pause` (full suite green), then `package.bat --no-pause` (`[test] OK`, `[build] OK`, `[package] OK` with the MSI present).

- [ ] **Step 3: Manual release checklist**

Fresh install of the MSI: welcome on first run, existing history and settings load, tray + hotkey + flyout + paste-back work, uninstall keeps `%LOCALAPPDATA%\ClipShelf` (Review Focus 5). Upgrade install over the WinUI build keeps history.

- [ ] **Step 4: Commit and request the merge**

```bash
git add -A
git commit -m "app: chore: retire the winui head, ship the msi from desktop"
```

Then stop: report the branch state and ask the owner for the merge decision. Do not push, do not merge without an explicit yes.
