# ClipShelf

ClipShelf is a Windows clipboard-history app that keeps recently copied text searchable and ready to paste from a floating flyout or the system tray.

## Features

- Capture copied text in a local SQLite history; search, pin, delete, or clear unpinned clips.
- Open the floating flyout with `Win+Shift+V` or a single tray click, then paste a clip into the previously active app.
- Browse the full history in a separate window, grouped into Pinned and Recent.
- Choose System, Light, or Dark theme and a Mica, Mica Alt, Acrylic, or solid backdrop where supported.
- Choose a filled or outline tray icon, hide the icon, and configure whether a tray double-click opens the full history window.
- Set the global shortcut by recording a key combination, run at Windows startup, and cap how many clips the history keeps; a search box in the title bar finds settings.
- Start in the notification area and keep history and settings on this PC under `%LOCALAPPDATA%\ClipShelf`.

The default shortcut is `Win+Shift+V`; change it in Settings if another app has already registered it.

## Requirements

- Windows 10 version 1809 or later, or Windows 11 (x64)
- For development: the .NET 10 SDK version pinned in `global.json`

## Scripts

| Script | Purpose |
|---|---|
| `run.bat` | Run the app in Debug (it starts in the notification area) |
| `build.bat` | Publish `ClipShelf.exe` to `artifacts\publish\win-x64` |
| `test.bat` | Run all tests |
| `package.bat` | Test, build and create the MSI in `artifacts\installer` |
| `clean.bat` | Remove all build output |

Pass `--no-pause` to skip the final key press (used by automation).

## Project structure

| Project | Responsibility |
|---|---|
| `src/ClipShelf.App` | WinUI 3 (Windows App SDK 1.x, unpackaged) UI, tray integration, clipboard flyout, and app startup |
| `src/ClipShelf.Core` | Domain models, interfaces, pure logic (no UI, no SQL) |
| `src/ClipShelf.Data` | SQLite + Dapper repositories and numbered SQL migrations |
| `tests/*` | xUnit v3 tests per project |
| `installer` | WiX v6 MSI |

Details: [docs/architecture.md](docs/architecture.md).

## Data locations

All user data is in `%LOCALAPPDATA%\ClipShelf\`:

| Path | Contents |
|---|---|
| `data.db` | Local SQLite clipboard-history database |
| `backups\` | Automatic backups (newest 14) |
| `logs\` | Daily log files (newest 14) |
| `cache\` | Rebuildable cache, safe to delete |
| `settings.json` | User settings |

Uninstalling keeps this folder. The database is local SQLite; database encryption is not included in the current version.

## Localization

The UI is currently English-only. `Strings.resx` (neutral) and `Strings.en.resx` contain matching English text and keys. Dates and numbers follow the system locale. See [docs/localization.md](docs/localization.md).

## Contributing

- Commit title: `<project>: <type>: <short title>`, 60 characters at most, imperative, English. Projects: `app`, `core`, `data`, `tests`, `installer`, `docs`, `repo`. Types: `feat`, `fix`, `perf`, `refactor`, `test`, `docs`, `build`, `chore`, `i18n`, `style`.
- Commit body: what changed and why, wrapped at 72 characters.
- Before committing, `build.bat` and `test.bat` must pass, and the affected docs must be updated.
- Decisions: [docs/decisions](docs/decisions).

## Releases

Version is set in `Directory.Build.props`. Process: [docs/release.md](docs/release.md). History: [CHANGELOG.md](CHANGELOG.md).

## License

No license has been specified for the repository yet. Bundled fonts are under the SIL Open Font License (`src/ClipShelf.App/Assets/Fonts/OFL.txt`).
