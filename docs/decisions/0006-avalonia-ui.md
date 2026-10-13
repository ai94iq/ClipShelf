# 0006. Avalonia UI in place of WinUI 3

Date: 2026-10-13
Status: Accepted

## Context

WinUI 3 runs only on Windows, and the owner wants ClipShelf on Linux and macOS as well. The view
models, services and storage were already portable (`ClipShelf.Presentation`, Core, Data); only
the views, the tray and the Windows glue were tied to WinUI. Avalonia was checked live on
2026-10-12 as the best fit for a C# desktop app across the three systems.

## Decision

The UI is Avalonia 12 (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`; tests use
`Avalonia.Headless`) in `src/ClipShelf.Desktop`. This replaces WinUI 3 / Windows App SDK in the
locked stack of ADR 0001, and Avalonia's own `TrayIcon` replaces `H.NotifyIcon.WinUI` (ADR 0002).
The rest of ADR 0001 stands.

## Consequences

- One UI codebase for every OS; Linux and macOS add their platform layer under `Platform/`.
- Fluent in Avalonia is v1 and lacks some WinUI pieces (ContentDialog, caption chrome, acrylic
  tint, Mica Alt); DESIGN.md lists each gap and how it is filled.
- The WinUI app (`src/ClipShelf.App`) and its tests were removed at the swap; they stay in git
  history before the merge of branch `avalonia`.
