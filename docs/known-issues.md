# Known issues

| Issue | Workaround | Status |
|---|---|---|
| Linux, Wayland: the global shortcut only works while a ClipShelf window has focus | Bind a system shortcut to `clipshelf --toggle` (Settings and the welcome window say so) | By design until the GlobalShortcuts portal is supported (ADR 0007) |
| Linux, Wayland: picking a clip copies it but cannot paste it into the other app | Press Ctrl+V; a hint says so after each pick | By design (ADR 0007) |
| Linux, Wayland: the source app name stays blank for copies from Wayland apps | None | No protocol exposes it |
| Linux, GNOME: no tray icon without the AppIndicator extension | Install the AppIndicator extension, or use History (it opens at launch) and `clipshelf --history` | By design |
| Linux: no keyring service (minimal desktops, WSL) | The database key goes to `~/.local/share/ClipShelf/key.bin`, readable only by your user; a warning is logged | By design |
| Linux: a locked keyring at start stops the app with "Unlock the keyring" | Unlock the keyring (it usually unlocks at sign-in) and start ClipShelf again | By design: a new key would make the history unreadable |
| Linux: tray icon, keyring storage, paste-back, flyout activation and the XWayland clipboard bridge on KDE and GNOME | None needed if they work | Not yet verified on a real desktop (developed on WSLg and Xvfb) |
