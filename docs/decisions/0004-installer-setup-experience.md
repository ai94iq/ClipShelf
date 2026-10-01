# 0004. Installer setup experience

Date: 2026-10-01
Status: Accepted

## Context

The installer started from the template: an `ar-SA` MSI wizard with the default WiX artwork, a
Start Menu shortcut and no finish-page options. The app then became English-only, and the first
release runs showed the wizard could not cover what the product needed: a readable license, a
visible installation folder, a desktop shortcut and the option to start the app when setup ends.

## Decision

Keep the per-machine WiX v6 MSI and the `WixUI_InstallDir` dialog set, and customise it:
`en-US` culture and language 1033, the MIT license in `License.rtf`, ClipShelf-branded dialog and
banner bitmaps (icon taken from the 256 px frame), an announced advertised shortcut in the exe's
component, a desktop shortcut, and a "Run ClipShelf" checkbox on the finish page that launches the
app through a one-line custom action.

## Consequences

- Windows Installer draws the wizard controls, so the finish-page checkbox keeps the stock
  light-grey control style; the artwork places it on a matching footer band instead of restyling it.
- The shortcut is installed for everyone; there is no per-user choice.
- `en-US` replaces the template's `ar-SA`, matching the app's English-only UI.
- Artwork is generated from `app.ico`; regenerating it needs the same 256 px frame extraction.
