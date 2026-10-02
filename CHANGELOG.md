# Changelog

All notable changes are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions follow [SemVer](https://semver.org/).

## [Unreleased]

## [0.3.2] - 2026-10-02

### Fixed
- Switching the Windows light/dark theme while ClipShelf runs now updates the tray icon at once; it used to keep the variant it picked at startup until a setting changed or the app restarted.

## [0.3.1] - 2026-10-02

### Fixed
- Capturing a clip no longer scans the whole history when trimming to the limit: with a large archive of pinned or categorized clips every copy could take seconds (7.6 s at 1 million clips); the trim now uses a partial index and costs a seek proportional to the limit (about 1 ms per copy at 1 million clips, and no more lock failures under rapid copying).

## [0.3.0] - 2026-10-02

### Added
- Rename and delete categories in Settings; deleting a category keeps its clips in the history without a category.
- The flyout is keyboard-first: ↑/↓ move through the clips, Enter pastes the selected one, and Ctrl+1…9 paste one of the first nine.
- Settings can check GitHub for a newer release — once per run or on demand — and links straight to the download.
- A one-time welcome window on the first run explains the hotkey and that ClipShelf lives in the tray.
- Copied images are kept too: rows show a thumbnail and picking one puts the image back on the clipboard; multi-copy and export stay text-only.

## [0.2.0] - 2026-10-02

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
- Categories: save clips into named groups from the row menu; the full history can filter by category, saved clips survive Clear, the history limit and retention, and exports include their category.
- Set a password in Settings; it is asked before exporting the history and before opening a locked category.
- Lock a category to keep its clips hidden from the history, the flyout, search and exports; unlock it with the password, and choose in Settings when locks return — on exit, on minimize or on shutdown/sign-out (a fresh password starts locked).
- Appearance: the Light theme paints pure white surfaces and there is a new full black theme; translucency stays available through the Background setting.

### Changed
- The installer shows the MIT license, an installation folder page, creates a desktop shortcut, and offers to run ClipShelf when finished; its artwork matches the app.
- Settings use grouped cards with taller rows, small section headings, and Enabled/Disabled toggles; the sidebar is an icon rail with Settings at the bottom, and the title bar holds a settings search that hides groups that do not match.
- The History page search box is a regular rounded field, and history rows no longer hover-highlight like a selection (the pin and delete buttons keep their own hover).
- Flyout monitor placement is centralized with the platform window helpers.
- The app icon is a stacked clipboard on a gradient tile, and the installer artwork uses it.
- The flyout opens next to the mouse pointer instead of the corner of the screen; with the pointer at the taskbar it still tucks into the corner.
- Database hardening: temporary storage stays in memory, SQLCipher scrubs freed memory, and a test proves the database, its sidecars and backups contain no readable clip text.
- Clip rows show readable app names: capitalized, with friendly names for common apps (Chrome, Edge, Word…).

### Fixed
- The minimize, maximize and close buttons and the row action icons stay visible in the light theme: the window frame and caption buttons follow the theme setting, and icons take the theme's secondary color (destructive buttons still tint red on hover).
- Locking categories (for example on minimize) updates the history filter at once, and picking the locked category again asks for the password; unlocking from the filter keeps the filter applied instead of falling back to the whole list.
- The flyout no longer flashes black when it opens — it is parked off-screen instead of hidden, so Windows never re-creates its acrylic surface — it closes when you click anywhere outside it, even when it never became the active window, and it hands focus back to the app you came from when it closes.
- Tray single-click now opens the flyout without waiting for double-click detection; clipboard-list updates preserve existing rows and avoid full-list redraws. Reopening also dismisses a stale clear-confirmation overlay.
- Changing the history size no longer trims on every keystroke or spin click, and the tray icon or shortcut are only re-applied when their own settings change.
- The history footer reads "1 clip" instead of "1 clips" for a single item.
- Installing a rebuilt installer now replaces the previous installation instead of adding another entry.
