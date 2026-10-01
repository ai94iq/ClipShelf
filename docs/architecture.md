# Architecture

## Projects and dependencies

App → Core, Data. Data → Core. Core → nothing.

- **ClipShelf.Core** (net10.0): models, interfaces, text helpers, caching helper, layout logic. Never WPF or SQL.
- **ClipShelf.Data** (net10.0): SQLite + Dapper repositories, migrations, backups. Never WPF.
- **ClipShelf.App** (net10.0-windows): views, view models, app services, startup, resources.

## Startup flow

Settings → culture → logging → single instance → host → backup and migrate → tray icon + clipboard watcher (no window opens at startup). Pages load their own data when navigated to.

## Data flow

ViewModel → repository (Task.Run + Dapper) → SQLite. Writes evict cache keys and send `DataChanged`; open pages reload.

## Loading states

Every page ViewModel derives from `PageViewModel` (Idle, Loading, Loaded, Empty, Error) and shows `LoadStateOverlay`.

## Caching layers

ViewModel state → identity maps → IMemoryCache (`GetOrLoadAsync`) → disk cache (`cache\`) → SQLite page cache.

## Feature map

| Feature | Folder | Notes |
|---|---|---|
| Clipboard history storage | src/ClipShelf.Data/Repositories/ClipRepository.cs | Text clips in SQLite; upsert by SHA-256 hash; keyset paging, search, pin, prune |
| Clipboard capture | src/ClipShelf.App/Services/ClipboardService.cs | Win32 clipboard listener; ignores the app's own writes; stores text with the source app name |
| Tray icon and lifetime | src/ClipShelf.App/Shell/AppShellService.cs | H.NotifyIcon tray icon and menu; closing a window hides it until the user exits |
| Clipboard history panel | src/ClipShelf.App/Features/ClipboardPanel/ | Borderless flyout above the taskbar; Win+Shift+V or the tray toggles it; search, pin, delete, clear, paste on select |
