# ClipShelf

TODO: one sentence on what the app does and who it's for.

## Requirements

- Windows 10 22H2 or Windows 11, x64
- For development: the .NET 10 SDK version pinned in `global.json`

## Scripts

| Script | Purpose |
|---|---|
| `run.bat` | Run the app in Debug |
| `build.bat` | Publish `ClipShelf.exe` to `artifacts\publish\win-x64` |
| `test.bat` | Run all tests |
| `package.bat` | Test, build and create the MSI in `artifacts\installer` |
| `clean.bat` | Remove all build output |

Pass `--no-pause` to skip the final key press (used by automation).

## Project structure

| Project | Responsibility |
|---|---|
| `src/ClipShelf.App` | WinUI 3 (Windows App SDK 1.x, unpackaged) UI: views, view models, startup, resources |
| `src/ClipShelf.Core` | Domain models, interfaces, pure logic (no UI, no SQL) |
| `src/ClipShelf.Data` | SQLite + Dapper repositories and migrations |
| `tests/*` | xUnit v3 tests per project |
| `installer` | WiX v6 MSI |

Details: [docs/architecture.md](docs/architecture.md).

## Data locations

All user data is in `%LOCALAPPDATA%\ClipShelf\`:

| Path | Contents |
|---|---|
| `data.db` | SQLite database |
| `backups\` | Automatic backups (newest 14) |
| `logs\` | Daily log files (newest 14) |
| `cache\` | Rebuildable cache, safe to delete |
| `settings.json` | User settings |

Uninstalling keeps this folder.

## Localization

Arabic is the default language and English is secondary. Strings live in `src/ClipShelf.App/Resources/Strings.resx` (Arabic) and `Strings.en.resx` (English) with identical keys. Use `{l:Tr Key}` in XAML and `Tr.Get("Key")` in C#. See [docs/localization.md](docs/localization.md).

## Contributing

- Commit title: `<project>: <type>: <short title>`, 60 characters at most, imperative, English. Projects: `app`, `core`, `data`, `tests`, `installer`, `docs`, `repo`. Types: `feat`, `fix`, `perf`, `refactor`, `test`, `docs`, `build`, `chore`, `i18n`, `style`.
- Commit body: what changed and why, wrapped at 72 characters.
- Before committing, `build.bat` and `test.bat` must pass, and the affected docs must be updated.
- Decisions: [docs/decisions](docs/decisions).

## Releases

Version is set in `Directory.Build.props`. Process: [docs/release.md](docs/release.md). History: [CHANGELOG.md](CHANGELOG.md).

## License

TODO: state the license. Bundled fonts are under the SIL Open Font License (`src/ClipShelf.App/Assets/Fonts/OFL.txt`).
