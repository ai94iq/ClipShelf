# Avalonia restart - Batch 9 (parity walk and swap)

The parity walk is done on branch `avalonia`. The swap waits for the owner's review of every
surface (spec, "Acceptance for the swap"), so this file is the checklist for it.

## Parity against the README feature list (2026-10-13)

| README feature | Avalonia app | How it was checked |
|---|---|---|
| Text capture, re-copy moves to top | yes (shared view model and store) | live capture, tests |
| Image capture, thumbnails, paste back as image | yes | live capture, DIB tests (BI_BITFIELDS fix) |
| History grouped Pinned / Recent, app names, relative times | yes | live screenshots, tests |
| Search, pin, delete, clear unpinned, empty states | yes | tests |
| Flyout by hotkey or a tray click, next to the pointer, closes on click elsewhere | yes | live (hotkey, --toggle), tests |
| Pick to paste back; Up/Down, Enter, Ctrl+1..9 | yes | guarded live paste, tests |
| Multi-select copy | yes | tests |
| Categories from the row folder button, create on the spot | yes | live menu in the flyout, tests |
| Rename and delete categories in Settings | yes | tests |
| Filter History by category; locked asks for the password | yes | tests |
| Lock a category; lock on exit, minimize, shutdown/sign-out | yes | tests (minimize added in batch 9) |
| Export JSON/CSV, password-guarded | yes; save picker not yet seen live | tests |
| Themes System, Light, Dark, Full black | yes | live walk |
| Backgrounds Mica, Mica Alt, Acrylic, solid | Mica Alt shows as Mica (Avalonia has one Mica) | live (Mica), tests |
| Tray icon filled/outline, hide it, double-click opens History | yes (double-click added in batch 9) | tests; tray clicks not yet seen live |
| Record a shortcut, run at startup, history cap, retention | yes | tests |
| Clear on sign-out, settings search | yes (search sits at the top of the window, not in a title bar) | tests |
| Update check, welcome window | yes | live, tests |
| SQLCipher + DPAPI, scrubbed memory, local data | unchanged shared code | existing Data tests |
| Arabic RTL | yes | live walk |

All four P1 behaviors are kept (true total and load-more, the copy notice, keyboard rows and
Esc, the hotkey warning).

## Known differences from main

- Mica Alt renders as Mica.
- The Settings search is at the top of the content, not in a custom title bar (the OS draws the
  title bar; see DESIGN.md, Window chrome).
- Dialogs are drawn inside their window (no separate dialog window), so the flyout never loses
  focus to one.

## Owner review before the swap

- [ ] Light, Dark, Full black and Arabic on every surface: History, Settings, flyout, dialogs,
  category menu, welcome.
- [ ] Tray: left click, double-click, menu items, Exit.
- [ ] Hotkey, text and image capture, paste-back, locks, export (save picker).

## Swap steps (after the review)

1. Merge `avalonia` into `main`.
2. Remove `src/ClipShelf.App` and `tests/ClipShelf.App.Tests`; drop them from `ClipShelf.slnx`.
3. Drop the Windows App SDK and `H.NotifyIcon` versions from `Directory.Packages.props`.
4. `build.bat` and `run.bat`: point at `src/ClipShelf.Desktop/ClipShelf.Desktop.csproj` (same
   publish flags; checked 2026-10-13: self-contained ReadyToRun publish runs, 227 MB versus 272 MB
   for the WinUI build).
5. `installer/Package.wxs`: the icon path moves to `src\ClipShelf.Desktop\Assets\app.ico`; the
   files are harvested from the publish folder by glob, so nothing else changes.
6. Docs: README (build and run commands), `docs/architecture.md`,
   `docs/decisions/0002-tray-icon-library.md` (superseded by Avalonia's tray).
7. `test.bat`, `build.bat`, `package.bat`; install the MSI on a clean profile and repeat the
   manual checks.
