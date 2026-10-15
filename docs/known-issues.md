# Known issues

| Issue | Workaround | Status |
|---|---|---|
| Linux, Wayland: the global shortcut only works while a Copyloft window has focus | Bind a system shortcut to `copyloft --toggle` (Settings and the welcome window say so) | By design until the GlobalShortcuts portal is supported (ADR 0007) |
| Linux, Wayland: picking a clip copies it but cannot paste it into the other app | Press Ctrl+V; a hint says so after each pick | By design (ADR 0007) |
| Linux, Wayland: the source app name stays blank for copies from Wayland apps | None | No protocol exposes it |
| Linux, GNOME: no tray icon without the AppIndicator extension | Install the AppIndicator extension, or use History (it opens at launch) and `copyloft --history` | By design |
| Linux: no keyring service (minimal desktops, WSL) | The database key goes to `~/.local/share/Copyloft/key.bin`, readable only by your user; a warning is logged | By design |
| Linux: a locked keyring at start stops the app with "Unlock the keyring" | Unlock the keyring (it usually unlocks at sign-in) and start Copyloft again | By design: a new key would make the history unreadable |
| macOS: the first launch is blocked (the app is not notarized) | System Settings > Privacy & Security > Open Anyway, or `xattr -dr com.apple.quarantine /Applications/Copyloft.app` | By design: no Apple Developer ID (ADR 0008) |
| macOS: picking a clip copies it but does not paste until Copyloft has the Accessibility permission | Allow Copyloft in Privacy & Security > Accessibility (macOS asks once), or press Cmd+V | By design |
| macOS: after an update, macOS asks for the login keychain password before Copyloft can read its database key | Enter it and choose Always Allow; the next update asks again | By design: each release is ad-hoc signed, and the Keychain trusts one signature (seen on macOS 15.8.1 in a VM) |
| Denying a keyring or Keychain request at start stops Copyloft | A window says so and names the log folder; start Copyloft again and allow access | By design: a new key would make the history unreadable |
| macOS: after an update, picking a clip copies it but does not paste, although Copyloft is still listed under Accessibility | In Privacy & Security > Accessibility, remove Copyloft with the minus button, pick a clip so macOS asks again, then switch it on | By design: the permission belongs to one ad-hoc signature (seen on macOS 15.8.1 in a VM) |
| Linux: tray icon, keyring storage, paste-back, flyout activation and the XWayland clipboard bridge on KDE and GNOME | None needed if they work | Not yet verified on a real desktop (developed on WSLg and Xvfb) |
