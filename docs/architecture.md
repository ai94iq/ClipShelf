# Architecture

## Projects and dependencies

App → Core, Data. Data → Core. Core → nothing.

- **ClipShelf.Core** (net10.0): models, interfaces, text helpers, caching helper, layout logic. Never WPF or SQL.
- **ClipShelf.Data** (net10.0): SQLite + Dapper repositories, migrations, backups. Never WPF.
- **ClipShelf.App** (net10.0-windows): views, view models, app services, startup, resources.

## Startup flow

Settings → culture → logging → single instance → host → backup and migrate → main window. Pages load their own data when navigated to.

## Data flow

ViewModel → repository (Task.Run + Dapper) → SQLite. Writes evict cache keys and send `DataChanged`; open pages reload.

## Loading states

Every page ViewModel derives from `PageViewModel` (Idle, Loading, Loaded, Empty, Error) and shows `LoadStateOverlay`.

## Caching layers

ViewModel state → identity maps → IMemoryCache (`GetOrLoadAsync`) → disk cache (`cache\`) → SQLite page cache.

## Feature map

| Feature | Folder | Notes |
|---|---|---|
