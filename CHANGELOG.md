# Changelog

All notable changes are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions follow [SemVer](https://semver.org/).

## [Unreleased]

### Added
- Initial project scaffold.
- Run in the notification area: a tray icon with Settings and Exit, and the app keeps running with no window open.
- Capture copied text into a local history (up to the configured limit), ignoring the app's own copies.
- Encrypt the history database (SQLCipher) with a Windows-protected key; an existing plaintext database is encrypted on the next launch.
- Show a floating clipboard flyout with Win+Shift+V or a left-click on the tray icon: search, choose an item to paste it, pin, delete or clear all.
- Use a clipboard icon for the app, and a tray icon that matches the light or dark taskbar.
- Open a full history window from the tray menu or the flyout to browse, search, pin, delete and clear clips.
- Full app window (NavigationView) with History and Settings pages; the history list is grouped into Pinned and Recent.
- Double-click the tray icon opens the full app; this can be turned off on the Settings page.
- New app icon, and a light/dark tray icon that follows the taskbar theme.
- Theme (system/light/dark) and background (Mica, Mica Alt, Acrylic, None) settings applied live on every window.
- Tray icon style (filled or outline) and a switch to hide the tray icon; an Exit button in Settings keeps the app reachable when the tray is hidden.
- Run at Windows startup, change the global shortcut by recording a key combination, and set how many clips the history keeps; lowering the limit trims the oldest unpinned clips.
- Keep clips for 1, 7 or 30 days (or forever), and optionally clear unpinned history when you sign out or shut down.
- A placeholder in the flyout and the history page when there are no clips, with a separate message when a search finds nothing; search is disabled until there are clips.
- Export the saved clips from Settings as a JSON or CSV file.
- Select several clips in the list and copy them all at once.

### Changed
- The installer shows the MIT license, an installation folder page, creates a desktop shortcut, and offers to run ClipShelf when finished; its artwork matches the app.
- Settings use grouped cards with taller rows, small section headings, and Enabled/Disabled toggles; the sidebar is an icon rail with Settings at the bottom, and the title bar holds a settings search that hides groups that do not match.
- The History page search box is a regular rounded field, and history rows no longer hover-highlight like a selection (the pin and delete buttons keep their own hover).
- Flyout monitor placement is centralized with the platform window helpers.
- Clip rows show readable app names: capitalized, with friendly names for common apps (Chrome, Edge, Word…).

### Fixed
- Tray single-click now opens the flyout without waiting for double-click detection; clipboard-list updates preserve existing rows and avoid full-list redraws. Reopening also dismisses a stale clear-confirmation overlay.
- Changing the history size no longer trims on every keystroke or spin click, and the tray icon or shortcut are only re-applied when their own settings change.
- The history footer reads "1 clip" instead of "1 clips" for a single item.
- Installing a rebuilt installer now replaces the previous installation instead of adding another entry.

### Removed
