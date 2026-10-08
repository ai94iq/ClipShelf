# ClipShelf — cross-platform migration design

Date: 2026-10-08
Status: Owner-approved design; ready for implementation planning
Supersedes: `plan-cross-platform.md` (2026-10-02) — the native-heads UI proposal is replaced by a
single Avalonia UI. That document's platform-capability analysis and fallback rules are carried
forward below.

## 1. Goal

One codebase, one UI, three platforms: ClipShelf runs on Windows, Linux and macOS as a .NET 10 +
Avalonia application. Every piece of base logic stays: clipboard capture, history, categories and
locks, export, settings, password gates and the protection model. The Windows app keeps shipping
on the current WinUI stack until the Avalonia app reaches feature parity, then the WinUI app is
removed.

Non-goals: mobile/web targets, cross-machine sync, Flatpak/Snap packages, Apple code signing and
notarization.

## 2. Owner decisions

| # | Decision |
|---|---|
| 1 | One Avalonia UI everywhere — Windows included. WinUI retires at parity. |
| 2 | Sequencing: Windows → Linux → macOS. Main receives the Avalonia app at Windows parity; Linux and macOS follow as releases. |
| 3 | The Avalonia rewrite lives on branch `cross-platform`. Phase 1 (extraction) lands on main first; it changes no behavior. |
| 4 | No Mac available: macOS is verified by CI (unsigned builds + unit tests); interactive verification is deferred and documented as unverified. |
| 5 | Linux dev/test bed: WSL (Ubuntu 26.04.1 LTS + WSLg — X11 and Wayland available, no GNOME/KDE shell). Real-desktop behaviors are marked verify-later with fallbacks. |
| 6 | The current visual design is replaced. A new visual world is designed with the impeccable replacement-world process as the opening of Phase 2; base logic and flows stay. The 2026-10-06 critique's four P1 issues are acceptance criteria. |
| 7 | Packaging: Windows MSI (existing pipeline); Linux tarball + `.deb` + `.rpm`; macOS unsigned `.app`/`.dmg`. No Apple Developer ID. |

## 3. Current state (evidence)

- `ClipShelf.Core` (~470 lines) and `ClipShelf.Data` (~470 lines) are portable `net10.0`;
  SQLCipher natives ship for linux-x64/arm64 and osx-x64/arm64.
- `ClipShelf.App` (~5,700 lines) is WinUI 3 / Windows App SDK (`net10.0-windows10.0.19041.0`,
  unpackaged, self-contained).
- Tests: 298 green (`test.bat`, xUnit v3 + Microsoft.Testing.Platform). App tests target Windows;
  Core/Data tests are portable.
- v0.3.1 shipped; main also carries unreleased fixes (hover work, clipboard-image crash fix).
- Windows-only pieces inside App: WinUI XAML and code-behind; tray (`H.NotifyIcon.WinUI`);
  `Platform/` Win32 (clipboard listener + message pump, global hotkey, paste-back, foreground
  app, flyout frame/placement, session watcher, system theme); DPAPI key protection; Run-key
  startup; Mutex single instance; `DispatcherQueue` use.
- Portable already (no WinUI types): view models, settings, category locks, export, password
  hashing, update check, date formatting, resx localization, `AppPaths`.

## 4. Target architecture

### Projects

```
src/ClipShelf.Core                   unchanged
src/ClipShelf.Data                   unchanged
src/ClipShelf.Presentation           NEW — net10.0
src/ClipShelf.Desktop                NEW — net10.0 (Avalonia; builds for Windows/macOS/Linux)
src/ClipShelf.App                    WinUI — until the swap, then removed
tests/ClipShelf.Core.Tests           unchanged
tests/ClipShelf.Data.Tests           unchanged
tests/ClipShelf.Presentation.Tests   NEW — portable view-model tests
tests/ClipShelf.Desktop.Tests        NEW — headless UI smoke tests
```

### ClipShelf.Presentation

Moves from App unchanged (namespace changes only): all view models
(`Features/*/*ViewModel.cs`, `PageViewModel`), settings service, category locks, export,
password hashing, update check, date formatting, resx localization, logging setup, DI
registration, pure layout logic (`PanelPlacement`, `PanelDismiss`).

New platform interfaces (implemented per OS by the head):

| Interface | Responsibility | Replaces |
|---|---|---|
| `IClipboardMonitor` | change events + read/write text and image | `Win32Clipboard`, message-pump clipboard part |
| `IGlobalHotkey` | register/unregister the global shortcut | `GlobalHotkey` |
| `IPasteBack` | remember the previous app, refocus it, send paste | `KeyboardPaste` + dismiss-target capture |
| `IForegroundApp` | active app name | `ForegroundApp` |
| `IKeyStore` | protect/unprotect the 32-byte DB key | `DatabaseKey` (DPAPI) |
| `IStartupRegistration` | run at sign-in (exists today) | Run key |
| `IUiDispatcher` | UI-thread marshalling | `DispatcherQueue` uses |
| `ISystemTheme` | dark/light + change event | `SystemTheme`, `SystemThemeWatcher` |
| `ISessionEnd` | sign-out/shutdown hook | `SessionWatcher` + session-cleanup branch |

Tray needs no interface: Avalonia's `TrayIcon` control covers Windows/macOS/Linux (Linux caveats
in §5).

### ClipShelf.Desktop

- `Views/` — flyout, history, settings, welcome, dialogs (rebuilt to the new design world, §6).
- `Themes/` — the new design system as Avalonia resources/styles.
- `Platform/Windows|Linux|MacOS/` — one folder per OS, all behind the interfaces above.
- Windows implementations: adapt today's `Platform/` Win32 code (clipboard listener, hotkey,
  paste-back, foreground app, frame placement, session end, theme) into the Avalonia host.

### Single instance and command surface

Replace the Windows Mutex with a named pipe (`NamedPipeServerStream`; Unix domain socket on
Linux/macOS). The pipe also carries commands: `clipshelf --toggle` and `clipshelf --history`
work on every OS. This is the documented fallback wherever global hotkeys need user binding.

### Data and settings compatibility

- Same per-OS locations via `AppPaths` (`%LOCALAPPDATA%` / `~/.local/share` /
  `~/Library/Application Support`).
- Windows upgrades keep the entire history and settings: same paths, same SQLCipher database,
  same `settings.json`.
- DB key: DPAPI on Windows (unchanged); Secret Service (libsecret) on Linux; Keychain on macOS.
  Missing keyring → `0600` key file + logged warning.
- No data migration is required on any platform.

## 5. Platform capability matrix and fallback rules

| Capability | Windows | Linux X11 | Linux Wayland | macOS |
|---|---|---|---|---|
| Tray icon | Avalonia `TrayIcon` | AppIndicator spec | KDE ✓ / GNOME needs the AppIndicator extension | Avalonia `TrayIcon` |
| Clipboard watch | Win32 listener (existing) | XFixes events (our interop) | KDE: `wl-paste --watch` ≥2.3.0 or data-control client / GNOME: **gap — spike decides** | pasteboard poll (~300 ms) |
| Global hotkey | RegisterHotKey (existing) | XGrabKey (our interop) | GlobalShortcuts portal (KDE 5.27+, GNOME 48+), else user-bound shortcut | Carbon hotkey, no permission needed |
| Paste into previous app | SendInput (existing) | XTest | copy-only by design | CGEventPost — needs Accessibility permission |
| Source app name | yes | yes | blank (no protocol) | yes |
| Flyout at cursor | yes | yes | compositor places it | non-activating panel |
| DB key protection | DPAPI | Secret Service via libsecret | same | Keychain |
| Run at sign-in | Run key | autostart `.desktop` | same | LaunchAgent |
| Session-end clear | existing hook | SIGTERM hook | same | power-off notification |

Fallback rules:

1. Hotkey unavailable → Settings shows "bind any key to `clipshelf --toggle`" guidance.
2. No tray (GNOME without extension; detected via missing StatusNotifierWatcher on D-Bus) →
   open History on launch + one-time hint; app stays alive via hotkey/`--toggle`.
3. Wayland paste-back impossible → copy-only + "Copied — press Ctrl+V" notice.
4. KDE Wayland clipboard watch requires wl-clipboard ≥ 2.3.0 → version detection + install
   guidance; a compiled ext-data-control client can remove the dependency later.
5. GNOME Wayland clipboard watch — no data-control protocol in Mutter; the Phase 3 spike decides
   between XWayland-bridge polling, a small GNOME Shell extension, or shipping without watch.
   Linux does not ship until this is resolved.
6. No keyring service → `0600` key file + warning (documented in `docs/known-issues.md`).
7. macOS paste-back needs the Accessibility permission → first-run explainer + system prompt.

WSL reality: WSLg provides X11 + Wayland and a clipboard bridge to Windows; no GNOME/KDE shell,
no tray host, no portals. WSL verifies: build, unit tests, app launch, X11 clipboard watch, basic
UI flows. It cannot verify: tray, portal hotkeys, GNOME/KDE Wayland behavior — those are
verify-later items on real desktops.

## 6. Design workstream (Phase 2 opening, impeccable replacement-world process)

1. `init` → PRODUCT.md: product truth, audience, what must remain untouched, what would make a
   polished result feel wrong. (No PRODUCT.md exists today; only the critique.)
2. Direction round: candidates derived from the audience's world, seeded roll, challengers judged
   on audience identification and product clarity; presented as a visual decision page with
   steer/re-roll. The owner locks a direction — this is the gate before any app view is built.
3. Direction contract recorded in the surface brief; the UI is then built fully committed to the
   chosen world. The flyout is the signature surface and must prove the mechanism; all windows
   (history, settings, welcome, dialogs) follow the same world.
4. Finish: impeccable critique/audit on the built app; DESIGN.md + `.impeccable/design.json`
   document the shipped world.

Constraints carried into the design brief: all base logic and flows stay; the four P1 critique
items are acceptance criteria; Arabic/RTL must be handled; icons remain our own vector
geometries (ported to Avalonia `Path` data); the bundled Noto Sans Arabic (OFL) stays.

P1 acceptance items (from the 2026-10-06 critique):

1. History must load beyond 50 items (incremental loading) and show a true total count.
2. Copying from History must give visible feedback.
3. History must be keyboard-operable (focus, selection, Enter) and Esc must cancel the
   clear-confirmation.
4. A failed hotkey rebind must keep the previous shortcut and warn inline.

## 7. Phases

### Phase 1 — Presentation extraction (behavior-free; merges to main)

- Deliverable: `ClipShelf.Presentation`; Win32 code becomes `Platform/Windows` implementations;
  named-pipe single instance + `--toggle`/`--history`; view-model tests move to
  `ClipShelf.Presentation.Tests` (portable).
- Acceptance: full suite green; MSI installs; app behaves exactly as before. Ships as a normal
  Windows release.

### Phase 2 — design world + Avalonia app to Windows parity + swap (branch `cross-platform`)

- Deliverable: §6 design gate passed; `ClipShelf.Desktop` with full UI (flyout, history,
  settings, welcome, dialogs), Windows platform layer, tray, single-instance wired.
- Acceptance: feature/behavior parity checklist walked against the README feature list; suite
  green including headless UI smoke tests; manual Windows verification (tray, hotkey, clipboard
  capture text+image, paste-back); impeccable finish review passed.
- Then: merge to main → Windows release; `ClipShelf.App`, Windows App SDK and `H.NotifyIcon`
  removed; MSI harvest updated.

### Phase 3 — Linux (feature branch off main)

- Deliverable: `Platform/Linux` — X11 clipboard watch (XFixes), KDE Wayland watch
  (wl-clipboard ≥ 2.3.0 or compiled client), hotkeys (XGrabKey + GlobalShortcuts portal), XTest
  paste-back, libsecret keyring, autostart `.desktop`, SIGTERM session hook, tray + no-tray
  detection; GNOME Wayland clipboard spike resolved; packaging (tarball + deb + rpm); ubuntu CI
  job.
- Acceptance: ubuntu CI green (build + tests); WSL smoke (capture, list, copy, flyout renders);
  real-desktop checklist documented as verify-later with fallbacks active. → Linux release.

### Phase 4 — macOS (feature branch off main; CI-only)

- Deliverable: `Platform/MacOS` — pasteboard poll, Carbon hotkey, CGEventPost + Accessibility
  explainer, Keychain, LaunchAgent, menu-bar-only app; unsigned `.app`/`.dmg` from macOS CI.
- Acceptance: macOS CI green (build + unit tests); bundle artifact produced and structurally
  validated; all interactive behavior explicitly marked unverified until a Mac exists.
  → macOS release (unsigned; README Gatekeeper note).

### Phase 5 — consolidation

Full CI matrix (windows/ubuntu/macos), `.sh` twins of the scripts, README per-OS install + limits
table, ADRs recorded, release docs and CHANGELOG updated.

## 8. Verification strategy

| Platform | Build | Unit tests | Interactive |
|---|---|---|---|
| Windows | local | local (full suite) | local — tray, hotkey, clipboard, paste-back |
| Linux | WSL + ubuntu CI | WSL + ubuntu CI | WSL smoke; real-desktop items verify-later |
| macOS | macOS CI | macOS CI | none — deferred, documented |

UI smoke tests: Avalonia.Headless runs basic UI flows in CI on all three OSes.

Existing discipline stays: the `test.bat` suite (with `.sh` twins added in Phase 5), MTP runner,
MSI install checks per release.

## 9. Packaging and release

- Windows: WiX MSI (existing pipeline; harvest updated at swap); winget flow unchanged; update
  checker unchanged. Windows requirement unchanged (10 version 1809+ / 11, x64).
- Linux: tarball (`.desktop` file + icons), `.deb`, `.rpm` built in CI from the publish output.
  Runtime notes: libsecret, wl-clipboard ≥ 2.3.0 (KDE Wayland), standard X11 libraries. No
  Flatpak/Snap — sandboxes block clipboard watching, hotkeys and paste-back.
- macOS: unsigned `.app` bundle + `.dmg` built on macOS CI; README documents the Gatekeeper
  right-click → Open step.
- Update checker links the per-OS release asset.

## 10. Risks

1. GNOME Wayland clipboard watch — the one genuine unknown; the Phase 3 spike decides; Linux
   ships after.
2. Avalonia 12 + .NET 10 macOS bundling quirk (documented workaround; fallback Avalonia 11.3.x).
3. Linux tray: Avalonia bug #13130 (icon can vanish) — own SNI fallback if it bites.
4. macOS unverifiable interactively — accepted; CI + review only.
5. New-design approval cycles — the direction gate before any view is built keeps scope bounded.
6. Dual UI during Phase 2 — short-lived; main stays frozen on WinUI.

## 11. Decisions to record; deferred items

ADRs to write during the work:

- Cross-platform stack: .NET 10 + Avalonia 12 (replaces/extends ADR 0001's WinUI lock).
- Tray: Avalonia `TrayIcon` (supersedes ADR 0002's H.NotifyIcon).
- Packaging: Linux tarball + deb + rpm; macOS unsigned (records decision 7).

Deferred (explicitly not planned): Apple Developer ID / notarization; Flatpak/Snap; the
background-effects model (decided by the design world in Phase 2); version numbers (set at
release time).
