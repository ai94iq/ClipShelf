# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

## Users

Public desktop users who discover the app through package managers (winget) and the release downloads for Linux and macOS and keep it running in the tray. Keyboard-curious rather than power users. They copy constantly during other work (writing, browsing, support, snippets) and need something they copied earlier to come back without breaking their flow.

## Product Purpose

Keep recently copied content (text and images) findable and re-pastable with zero ceremony. Copyloft exists so the clipboard stops being a single slot. Success means public adoption on all three platforms: installs that people keep running.

## Positioning

A clipboard shelf that pastes back into the app you came from: the flyout opens next to the cursor, and picking a clip restores the previous window and pastes. The protection model is clipboard-specific, not generic settings: pinned clips survive clearing, categories survive limits and retention, and a locked category hides its clips behind one password that also guards exports. Everything stays local and encrypted.

## Operating Context

Native desktop application on Windows, Linux and macOS, one visual identity across all three; platform integration adapts where the OS requires it (tray, shortcuts, permissions). Tray-first: nothing opens at startup. The default shortcut is Win+Shift+V (Cmd+Shift+V on macOS) or a tray click. Arabic with RTL is a shipped locale, selectable in Settings. Used mid-task: the flyout appears while the user is working in another app and closes when they leave.

## Capabilities and Constraints

- Text and image history in SQLCipher-encrypted local storage; the key is protected per OS (DPAPI, then Secret Service, then Keychain).
- Upsert by hash (re-copying moves a clip to the top), pin, delete with undo, clear unpinned, retention (1/7/30/forever), clear on sign-out.
- Categories with optional password locks; one password guards locks and exports; lock timing (on exit, minimize, shutdown).
- Export to JSON/CSV; settings search; themes and accent; tray icon options; update check; welcome window on first run.
- Local-only: no cloud, no sync, no accounts. Sandboxed packages (Flatpak/Snap) are excluded because they block clipboard watching, hotkeys and paste-back.
- One UI for all three platforms on .NET 10 and Avalonia 12 (ADR 0006, 0007, 0008); background effects (Mica, Acrylic) are Windows only.

## Brand Commitments

- The name Copyloft (formerly ClipShelf, renamed because other clipboard apps use that name) stays everywhere users look. The old name survives only in identifiers on disk (exe and bundle names, data folder, keychain entry, winget id), so existing installs keep their history.
- Quiet and out of the way: nothing opens unless asked, no nagging, no interruptions. The app earns attention only when invoked.
- MIT license.

## Evidence on Hand

- README.md, docs/architecture.md, docs/performance.md and the shipped screenshots in docs/images (the old design; evidence of the subject, not authority over the new world).
- A local design critique (2026-10-06, 29/40, not in the repository; its four P1 issues were acceptance criteria for the Avalonia build).
- No testimonials, benchmarks or press beyond the repo's own performance notes; future work must not fabricate them.

## Product Principles

1. Zero ceremony: the clipboard is a reflex; the whole job is one gesture away (hotkey, click, Enter).
2. Out of the way by default: never open a window uninvited; appear when invoked and leave when done.
3. Private by construction: local-only and encrypted; the protection model is part of the product, not buried settings.
4. Trustworthy at a glance: confirm what happened (a copy, a delete) and say plainly when something failed (a hotkey another app took).
5. Keyboard-first, mouse welcome: every flow is reachable from the keys; nothing requires them.

## Accessibility & Inclusion

Arabic/RTL is first-class, not an afterthought. Keyboard reachability, screen-reader naming for icon-only controls, and contrast minimums are requirements (the critique's accessibility findings are acceptance criteria). No interaction may depend on motion.
