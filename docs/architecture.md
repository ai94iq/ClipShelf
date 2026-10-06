# Architecture

## Projects and dependencies

App → Core, Data. Data → Core. Core → nothing.

- **ClipShelf.Core** (net10.0): models, interfaces, text helpers, layout logic. Never WPF or SQL.
- **ClipShelf.Data** (net10.0): SQLite + Dapper repositories, migrations, backups. Never WPF.
- **ClipShelf.App** (net10.0-windows): views, view models, app services, startup, resources.

## Startup flow

Settings → culture → logging → single instance → host → backup and migrate → startup registration and saved shortcut applied → tray icon + clipboard watcher (no window opens at startup). Pages load their own data when navigated to.

## Data flow

ViewModel → repository (Task.Run + Dapper) → SQLite (SQLCipher-encrypted; the key is protected with Windows DPAPI). Writes send `DataChanged`; open pages reload. The clipboard panel refreshes quietly after appearing and reconciles rows in place to avoid a loading flash or full-list redraw. Retention is enforced on launch, after each capture, and whenever the setting changes.

## Loading states

Every page ViewModel derives from `PageViewModel` (Idle, Loading, Loaded, Empty, Error) and shows `LoadStateOverlay`.

## Caching

No app-level cache yet: the history is small, repositories read SQLite directly, and every write notifies open views. A feature that needs caching uses `IMemoryCache` with `GetOrLoadAsync` as ADR 0001 specifies (see ADR 0005).

## Feature map

| Feature | Folder | Notes |
|---|---|---|
| Clipboard history storage | src/ClipShelf.Data/Repositories/ClipRepository.cs | Text clips in SQLite; upsert by SHA-256 hash; keyset paging, search, pin, prune |
| Clipboard capture | src/ClipShelf.App/Services/ClipboardService.cs | Win32 clipboard listener; ignores the app's own writes; stores text with the source app name |
| Tray icon and lifetime | src/ClipShelf.App/Shell/AppShellService.cs | H.NotifyIcon tray icon and menu; closing a window hides it until the user exits |
| Clipboard history panel | src/ClipShelf.App/Features/ClipboardPanel/ | Borderless flyout above the taskbar; native frame and monitor placement are in `Platform/PanelFrame.cs`; Win+Shift+V or an immediate tray click toggles it; search, pin, delete, clear, paste on select; history rows update in place |
| App shell | src/ClipShelf.App/Shell/MainWindow.xaml | NavigationView window with the History and Settings pages; closing only hides it |
| History page | src/ClipShelf.App/Features/History/HistoryPage.xaml | Full list grouped into Pinned and Recent, search, clip count, clear all with a confirmation dialog |
| Settings page | src/ClipShelf.App/Features/Settings/SettingsPage.xaml | Appearance, tray, behavior and history (retention, sign-out clear) options, with a search box in the title bar |
