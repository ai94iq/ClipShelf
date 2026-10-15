# Known issues

| Issue | Workaround | Status |
|---|---|---|
| Linux, Wayland: the global shortcut only works while a ClipShelf window has focus | Bind a system shortcut to `clipshelf --toggle` (Settings and the welcome window say so) | By design until the GlobalShortcuts portal is supported (ADR 0007) |
| Linux, Wayland: picking a clip copies it but cannot paste it into the other app | Press Ctrl+V; a hint says so after each pick | By design (ADR 0007) |
| Linux, Wayland: the source app name stays blank for copies from Wayland apps | None | No protocol exposes it |
| Linux, GNOME: no tray icon without the AppIndicator extension | Install the AppIndicator extension, or use History (it opens at launch) and `clipshelf --history` | By design |
| Linux: no keyring service (minimal desktops, WSL) | The database key goes to `~/.local/share/ClipShelf/key.bin`, readable only by your user; a warning is logged | By design |
| Linux: a locked keyring at start stops the app with "Unlock the keyring" | Unlock the keyring (it usually unlocks at sign-in) and start ClipShelf again | By design: a new key would make the history unreadable |
| macOS: the first launch is blocked (the app is not notarized) | System Settings > Privacy & Security > Open Anyway, or `xattr -dr com.apple.quarantine /Applications/ClipShelf.app` | By design: no Apple Developer ID (ADR 0008) |
| macOS: picking a clip copies it but does not paste until ClipShelf has the Accessibility permission | Allow ClipShelf in Privacy & Security > Accessibility (macOS asks once), or press Cmd+V | By design |
| macOS: the menu-bar icon, the flyout, paste-back and the LaunchAgent at login | None needed if they work | Not yet verified on a Mac; only CI runs the macOS code (ADR 0008) |
| Linux: tray icon, keyring storage, paste-back, flyout activation and the XWayland clipboard bridge on KDE and GNOME | None needed if they work | Not yet verified on a real desktop (developed on WSLg and Xvfb) |
