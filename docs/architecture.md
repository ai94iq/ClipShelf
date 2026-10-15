# Architecture

## Projects and dependencies

Desktop depends on Presentation, Core and Data. Presentation depends on Core and Data. Data depends on Core. Core depends on nothing.

- **ClipShelf.Core** (net10.0): models, interfaces, text helpers, layout logic. Never UI or SQL.
- **ClipShelf.Data** (net10.0): SQLite + Dapper repositories, migrations, backups. Never UI.
- **ClipShelf.Presentation** (net10.0): view models, portable services, settings, localization and the platform interfaces. No UI framework types.
- **ClipShelf.Desktop** (net10.0): Avalonia views, the platform implementations of the Presentation interfaces (`Platform/Windows`, `Platform/Linux` and `Platform/MacOS`, chosen at startup in `Hosting/ServiceRegistration.cs`), startup and resources. See ADR 0006, 0007, 0008 and DESIGN.md.

## Startup flow

Settings, culture, logging (unhandled exceptions are logged and flushed), single instance (named pipe, plus a lock file off Windows; a second launch sends `--toggle` or `--history` and exits), on macOS a wait for the display to wake (Avalonia's render timer needs one), host, backup and migrate (a failed start shows `StartupErrorWindow`), startup registration and saved shortcut applied, tray icon and clipboard watcher (no window opens at startup, except History on a desktop with no tray). Pages load their own data when navigated to.

## Data flow

ViewModel, repository (Task.Run + Dapper), SQLite (SQLCipher-encrypted; the key is protected with Windows DPAPI, or kept in the Secret Service keyring on Linux and the Keychain on macOS). Writes send `DataChanged`; open pages reload. The clipboard panel refreshes quietly after appearing and reconciles rows in place to avoid a loading flash or full-list redraw. Retention is enforced on launch, after each capture, and whenever the setting changes.

## Loading states

Every page ViewModel derives from `PageViewModel` (Idle, Loading, Loaded, Empty, Error); views show each state through the `LoadStateIs` converter.

## Caching

No app-level cache yet: the history is small, repositories read SQLite directly, and every write notifies open views. A feature that needs caching uses `IMemoryCache` with `GetOrLoadAsync` as ADR 0001 specifies (see ADR 0005).

## Feature map

| Feature | Folder | Notes |
|---|---|---|
| Clipboard history storage | src/ClipShelf.Data/Repositories/ClipRepository.cs | Text clips in SQLite; upsert by SHA-256 hash; keyset paging, search, pin, prune |
| Clipboard capture | src/ClipShelf.Desktop/Platform/ClipboardService.cs | Shared capture rules over a per-OS backend (`Windows/Win32ClipboardBackend.cs`, `AvaloniaClipboardBackend.cs` notified by XFixes on Linux and a pasteboard poll on macOS); ignores the app's own writes; stores text and images with the source app name |
| Tray icon and lifetime | src/ClipShelf.Desktop/Shell/AppShellService.cs | Avalonia tray icon and menu, click and double-click; the hotkey; closing a window hides it until the user exits |
| Clipboard history panel | src/ClipShelf.Desktop/Shell/FlyoutWindow.axaml | Flyout next to the pointer (placement in Presentation `Platform/PanelPlacement.cs`); the hotkey or a tray click toggles it; search, pin, delete, clear, paste on select; history rows update in place |
| App shell | src/ClipShelf.Desktop/Shell/MainWindow.axaml | Content runs into the title bar (system traffic lights on macOS); a compact rail switches History and Settings; closing only hides it |
| History page | src/ClipShelf.Desktop/Features/HistoryPage.axaml | Full list grouped into Pinned and Recent, search, category filter, clip count, clear all with a confirmation |
| Settings page | src/ClipShelf.Desktop/Features/SettingsPage.axaml | Appearance, tray, behavior, history, export, categories and updates, with a search box in the title bar |
| Linux platform | src/ClipShelf.Desktop/Platform/Linux | One X11 connection on its own thread (XFixes, key grabs, XTest), libsecret key store, XDG autostart, SIGTERM session end; Wayland sessions run through XWayland with copy-only paste (ADR 0007) |
| macOS platform | src/ClipShelf.Desktop/Platform/MacOS | Pasteboard changeCount poll, Carbon hotkey, CGEvent paste behind the Accessibility permission, Keychain, LaunchAgent; `MacAppPresence` makes the app regular (its menu and a Dock icon) while the main window is open (ADR 0008) |
| Dialogs | src/ClipShelf.Desktop/Controls/InlineDialog.axaml | Dialogs drawn inside their window (the flyout must keep focus); password prompts in `Features/PasswordPrompt.cs` |
