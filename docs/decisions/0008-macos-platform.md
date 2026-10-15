# 0008. macOS platform layer

Date: 2026-10-15
Status: Accepted

## Context
After Linux (ADR 0007) the same platform interfaces needed macOS implementations. There is no
Mac to develop or test on, only GitHub's macOS runners, and no Apple Developer ID to sign or
notarize with.

## Decision
- Clipboard: poll the general pasteboard's changeCount every 300 ms (macOS has no change
  notification); read and write through Avalonia's clipboard, images as public.png.
- Hotkey: Carbon RegisterEventHotKey, which needs no permission. The Windows key is Command;
  F21 to F24 do not exist on a Mac.
- Paste-back: Cmd+V through CGEventPost, which needs the Accessibility permission. Without it the
  pick is copied with a hint, and macOS is asked once (AXIsProcessTrustedWithOptions).
- Key: the login Keychain behind the same KeyringKeyStore rules as Linux.
- Run at login: a per-user LaunchAgent. Session end: SIGTERM and Avalonia's ShutdownRequested.
- A menu-bar app: no Dock icon at launch (ShowInDock off). While the main window is open it
  becomes a regular app (Dock icon, its own menu next to the Apple menu) and goes back when the
  window closes. The bundle sets no LSUIElement: with it, macOS ignored the policy switch.
- Single instance: the socket file is a short hashed path, since macOS caps socket paths at 104
  characters and its per-user temp directory is long.
- Packages: an ad-hoc-signed ClipShelf.app and a .dmg per architecture (arm64, x64), minimum
  macOS 15 (the oldest the .NET 10 supported-OS list still supports).

## Consequences
Everything native is checked only on CI: live tests there exercise the Keychain, the pasteboard,
the frontmost app and pointer, and Carbon registration; the packaging step validates each
bundle. Interactive behavior (the menu-bar icon, the flyout, paste-back with the permission) is
unverified until someone runs it on a Mac. Without notarization, Gatekeeper blocks the first
launch until the user allows it (README).
