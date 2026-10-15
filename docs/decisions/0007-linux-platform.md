# 0007. Linux platform layer

Date: 2026-10-15
Status: Accepted

## Context
Copyloft runs on Avalonia (ADR 0006), so Linux needs only the platform services: clipboard
watch, global hotkey, paste-back, the active app, the database key, autostart, session end and
the tray. Linux desktops split into X11 and Wayland sessions, and Wayland has no shared protocol
for a global hotkey, for sending keys to another app, or (on GNOME) for watching the clipboard.
Development and CI have WSLg (Weston with XWayland) and Xvfb, not a GNOME or KDE desktop.

## Decision
One X11 layer serves both session types, because Avalonia itself runs through XWayland on
Wayland:
- Clipboard: XFixes owner events on CLIPBOARD, reads and writes through Avalonia's clipboard.
  On Wayland the XWayland bridge mirrors Wayland-side copies to X (seen in WSLg); no separate
  `wl-paste` watcher. This settles the GNOME clipboard question in favor of the bridge.
- Hotkey: XGrabKey on the root window. On Wayland it only fires while an X window has focus, so
  Settings and the welcome window explain binding a system shortcut to `copyloft --toggle`.
  The GlobalShortcuts portal is left for later.
- Paste-back: XTest Ctrl+V on X11; on Wayland the pick is copied and a hint says to press Ctrl+V.
- Key: Secret Service through libsecret; without a keyring a 0600 key file and a warning. A key
  marked as being in the keyring is never replaced when the keyring does not answer.
- Tray: Avalonia's TrayIcon (StatusNotifierItem); without a StatusNotifierWatcher on D-Bus the
  app opens History at launch.
- Single instance: a flock lock file in XDG_RUNTIME_DIR claims the name (the pipe is a Unix
  socket another process could re-bind), and the name is per user only.
- Packages: tarball, .deb and .rpm from one staged tree; no Flatpak or Snap, whose sandboxes
  block clipboard watching and key input.

## Consequences
One code path to maintain and test (live X11 tests run under Xvfb in CI). Wayland users bind
their own shortcut and paste themselves; a portal hotkey and a Wayland paste path would need
real KDE and GNOME machines to build against. Behavior on real desktops (tray, keyring,
activation, the bridge on GNOME and KDE) is verify-later, listed in docs/known-issues.md.
